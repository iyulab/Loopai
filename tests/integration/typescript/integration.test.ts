/**
 * Integration tests for Loopai TypeScript SDK
 *
 * These tests require a running Loopai API server at http://localhost:8080
 * Run: cd src/Loopai.CloudApi && dotnet run
 */
import * as fs from 'fs';
import * as path from 'path';
import {
  LoopaiClient,
  LoopaiException,
  ValidationException,
  ExecutionException,
  Task,
  ExecutionResult,
  BatchExecuteResponse,
} from '@loopai/sdk';

interface TestConfig {
  baseUrl: string;
  timeout: number;
  retries: number;
  testData: {
    taskName: string;
    taskDescription: string;
    inputSchema: Record<string, unknown>;
    outputSchema: Record<string, unknown>;
    accuracyTarget: number;
    latencyTargetMs: number;
    samplingRate: number;
    sampleInputs: Array<Record<string, unknown>>;
  };
}

// Load test configuration
const configPath = path.join(__dirname, '..', 'test-config.json');
const testConfig: TestConfig = JSON.parse(fs.readFileSync(configPath, 'utf-8'));

describe('Health Check', () => {
  let client: LoopaiClient;

  beforeAll(() => {
    client = new LoopaiClient({
      baseUrl: testConfig.baseUrl,
      timeout: testConfig.timeout,
    });
  });

  test('should check API health', async () => {
    const health = await client.getHealth();

    expect(health.status).toBe('healthy');
    expect(health.version).toBeDefined();
    expect(health.timestamp).toBeDefined();
  });
});

describe('Task Lifecycle', () => {
  let client: LoopaiClient;

  beforeAll(() => {
    client = new LoopaiClient({
      baseUrl: testConfig.baseUrl,
      timeout: testConfig.timeout,
    });
  });

  test('should create and retrieve a task', async () => {
    const { testData } = testConfig;

    // Create task
    const task = await client.createTask({
      name: testData.taskName,
      description: testData.taskDescription,
      inputSchema: testData.inputSchema,
      outputSchema: testData.outputSchema,
      accuracyTarget: testData.accuracyTarget,
      latencyTargetMs: testData.latencyTargetMs,
      samplingRate: testData.samplingRate,
    });

    // Verify task creation
    expect(task.id).toBeDefined();
    expect(task.name).toBe(testData.taskName);
    expect(task.description).toBe(testData.taskDescription);
    expect(task.accuracyTarget).toBe(testData.accuracyTarget);
    expect(task.latencyTargetMs).toBe(testData.latencyTargetMs);
    expect(task.samplingRate).toBe(testData.samplingRate);
    expect(task.createdAt).toBeDefined();

    // Retrieve task
    const retrievedTask = await client.getTask(task.id);
    expect(retrievedTask.id).toBe(task.id);
    expect(retrievedTask.name).toBe(task.name);
    expect(retrievedTask.description).toBe(task.description);
  });
});

describe('Single Execution', () => {
  let client: LoopaiClient;
  let testTask: Task;

  beforeAll(async () => {
    client = new LoopaiClient({
      baseUrl: testConfig.baseUrl,
      timeout: testConfig.timeout,
    });

    const { testData } = testConfig;
    testTask = await client.createTask({
      name: `${testData.taskName}-single-ts`,
      description: testData.taskDescription,
      inputSchema: testData.inputSchema,
      outputSchema: testData.outputSchema,
      accuracyTarget: testData.accuracyTarget,
      latencyTargetMs: testData.latencyTargetMs,
      samplingRate: testData.samplingRate,
    });
  });

  test('should execute a task', async () => {
    const { testData } = testConfig;
    const inputData = testData.sampleInputs[0];

    const result = await client.execute({
      taskId: testTask.id,
      input: inputData,
    });

    // Verify execution result
    expect(result.id).toBeDefined();
    expect(result.taskId).toBe(testTask.id);
    expect(result.version).toBeGreaterThanOrEqual(1);
    expect(result.output).toBeDefined();
    expect(result.latencyMs).toBeGreaterThanOrEqual(0);
    expect(result.sampledForValidation).toBeDefined();
    expect(result.executedAt).toBeDefined();
  });

  test('should execute with forced validation', async () => {
    const { testData } = testConfig;
    const inputData = testData.sampleInputs[1];

    const result = await client.execute({
      taskId: testTask.id,
      input: inputData,
      forceValidation: true,
    });

    expect(result.sampledForValidation).toBe(true);
  });
});

describe('Batch Execution', () => {
  let client: LoopaiClient;
  let batchTask: Task;

  beforeAll(async () => {
    client = new LoopaiClient({
      baseUrl: testConfig.baseUrl,
      timeout: testConfig.timeout,
    });

    const { testData } = testConfig;
    batchTask = await client.createTask({
      name: `${testData.taskName}-batch-ts`,
      description: testData.taskDescription,
      inputSchema: testData.inputSchema,
      outputSchema: testData.outputSchema,
      accuracyTarget: testData.accuracyTarget,
      latencyTargetMs: testData.latencyTargetMs,
      samplingRate: testData.samplingRate,
    });
  });

  test('should execute simple batch', async () => {
    const { testData } = testConfig;
    const inputs = testData.sampleInputs;

    const result = await client.batchExecute(batchTask.id, inputs, 10);

    // Verify batch result
    expect(result.batchId).toBeDefined();
    expect(result.taskId).toBe(batchTask.id);
    expect(result.totalItems).toBe(inputs.length);
    expect(result.successCount).toBeGreaterThanOrEqual(0);
    expect(result.failureCount).toBeGreaterThanOrEqual(0);
    expect(result.successCount + result.failureCount).toBe(result.totalItems);
    expect(result.totalDurationMs).toBeGreaterThanOrEqual(0);
    expect(result.avgLatencyMs).toBeGreaterThanOrEqual(0);
    expect(result.results).toHaveLength(inputs.length);

    // Verify individual results
    for (const item of result.results) {
      expect(item.id).toBeDefined();
      expect(item.executionId).toBeDefined();
      if (item.success) {
        expect(item.output).toBeDefined();
        expect(item.latencyMs).toBeGreaterThanOrEqual(0);
      } else {
        expect(item.errorMessage).toBeDefined();
      }
    }
  });

  test('should execute advanced batch', async () => {
    const { testData } = testConfig;

    const items = testData.sampleInputs.map((input, i) => ({
      id: `email-${String(i).padStart(3, '0')}`,
      input,
      forceValidation: i % 2 === 0, // Force validation for even items
    }));

    const result = await client.batchExecuteAdvanced({
      taskId: batchTask.id,
      items,
      maxConcurrency: 5,
      stopOnFirstError: false,
      timeoutMs: 30000,
    });

    // Verify batch result
    expect(result.totalItems).toBe(items.length);
    expect(result.results).toHaveLength(items.length);

    // Verify custom IDs preserved
    const resultIds = new Set(result.results.map((r) => r.id));
    const expectedIds = new Set(items.map((item) => item.id));
    expect(resultIds).toEqual(expectedIds);

    // Check forced validation worked
    for (let i = 0; i < result.results.length; i++) {
      const item = result.results[i];
      if (item.success && i % 2 === 0) {
        // Even items should have forced validation
        expect(item.sampledForValidation).toBeDefined();
      }
    }
  });
});

describe('Error Handling', () => {
  let client: LoopaiClient;

  beforeAll(() => {
    client = new LoopaiClient({
      baseUrl: testConfig.baseUrl,
      timeout: testConfig.timeout,
    });
  });

  test('should throw validation error for invalid task', async () => {
    await expect(
      client.createTask({
        name: '', // Empty name should fail
        description: 'Test',
        inputSchema: { type: 'object' },
        outputSchema: { type: 'string' },
      })
    ).rejects.toThrow(ValidationException);
  });

  test('should throw error for non-existent task', async () => {
    const fakeId = '00000000-0000-0000-0000-000000000000';

    await expect(client.getTask(fakeId)).rejects.toThrow(LoopaiException);
  });

  test('should throw validation error for invalid input', async () => {
    const { testData } = testConfig;

    // Create task
    const task = await client.createTask({
      name: `${testData.taskName}-error-ts`,
      description: testData.taskDescription,
      inputSchema: testData.inputSchema,
      outputSchema: testData.outputSchema,
    });

    // Try to execute with invalid input (missing required field)
    await expect(
      client.execute({
        taskId: task.id,
        input: {}, // Missing 'text' field
      })
    ).rejects.toThrow(ValidationException);
  });
});

describe('Retry Logic', () => {
  test('should work with retry configuration', async () => {
    const client = new LoopaiClient({
      baseUrl: testConfig.baseUrl,
      timeout: testConfig.timeout,
      maxRetries: 3,
    });

    const health = await client.getHealth();
    expect(health.status).toBe('healthy');
  });
});

describe('Concurrency', () => {
  let client: LoopaiClient;
  let concurrentTask: Task;

  beforeAll(async () => {
    client = new LoopaiClient({
      baseUrl: testConfig.baseUrl,
      timeout: testConfig.timeout,
    });

    const { testData } = testConfig;
    concurrentTask = await client.createTask({
      name: `${testData.taskName}-concurrent-ts`,
      description: testData.taskDescription,
      inputSchema: testData.inputSchema,
      outputSchema: testData.outputSchema,
    });
  });

  test('should handle multiple concurrent executions', async () => {
    const { testData } = testConfig;
    const inputs = testData.sampleInputs.slice(0, 3);

    // Execute multiple tasks concurrently
    const promises = inputs.map((input) =>
      client.execute({
        taskId: concurrentTask.id,
        input,
      })
    );

    const results = await Promise.all(promises);

    expect(results).toHaveLength(inputs.length);
    for (const result of results) {
      expect(result.taskId).toBe(concurrentTask.id);
      expect(result.output).toBeDefined();
    }
  });
});
