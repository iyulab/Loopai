/**
 * Unit tests for LoopaiClient
 */

import axios from 'axios';
import MockAdapter from 'axios-mock-adapter';
import { LoopaiClient } from '../src/client';
import {
  LoopaiException,
  ValidationException,
  ExecutionException,
  ConnectionException,
} from '../src/exceptions';

describe('LoopaiClient', () => {
  let mock: MockAdapter;
  let client: LoopaiClient;

  beforeEach(() => {
    mock = new MockAdapter(axios);
    client = new LoopaiClient({
      baseUrl: 'http://test-api.loopai.dev',
      maxRetries: 0, // Disable retries for tests
    });
  });

  afterEach(() => {
    mock.restore();
  });

  describe('constructor', () => {
    it('should initialize with default options', () => {
      const defaultClient = new LoopaiClient();
      expect(defaultClient).toBeInstanceOf(LoopaiClient);
    });

    it('should initialize with custom options', () => {
      const customClient = new LoopaiClient({
        baseUrl: 'https://custom-api.loopai.dev',
        apiKey: 'test-key',
        timeout: 60000,
        maxRetries: 5,
      });
      expect(customClient).toBeInstanceOf(LoopaiClient);
    });
  });

  describe('getHealth', () => {
    it('should return health status', async () => {
      const healthResponse = {
        status: 'healthy',
        version: '1.0.0',
        timestamp: new Date().toISOString(),
      };

      mock.onGet('/health').reply(200, healthResponse);

      const health = await client.getHealth();
      expect(health.status).toBe('healthy');
      expect(health.version).toBe('1.0.0');
    });
  });

  describe('createTask', () => {
    it('should create a task successfully', async () => {
      const taskResponse = {
        id: '550e8400-e29b-41d4-a716-446655440000',
        name: 'test-task',
        description: 'Test task',
        inputSchema: { type: 'object' },
        outputSchema: { type: 'string' },
        accuracyTarget: 0.9,
        latencyTargetMs: 100,
        samplingRate: 0.1,
        createdAt: new Date().toISOString(),
      };

      mock.onPost('/api/v1/tasks').reply(200, taskResponse);

      const task = await client.createTask({
        name: 'test-task',
        description: 'Test task',
        inputSchema: { type: 'object' },
        outputSchema: { type: 'string' },
      });

      expect(task.id).toBe('550e8400-e29b-41d4-a716-446655440000');
      expect(task.name).toBe('test-task');
      expect(task.accuracyTarget).toBe(0.9);
    });
  });

  describe('getTask', () => {
    it('should retrieve a task by ID', async () => {
      const taskId = '550e8400-e29b-41d4-a716-446655440000';
      const taskResponse = {
        id: taskId,
        name: 'test-task',
        description: 'Test task',
        inputSchema: { type: 'object' },
        outputSchema: { type: 'string' },
        accuracyTarget: 0.9,
        latencyTargetMs: 100,
        samplingRate: 0.1,
        createdAt: new Date().toISOString(),
      };

      mock.onGet(`/api/v1/tasks/${taskId}`).reply(200, taskResponse);

      const task = await client.getTask(taskId);
      expect(task.id).toBe(taskId);
      expect(task.name).toBe('test-task');
    });

    it('should throw LoopaiException for 404 errors', async () => {
      const taskId = '550e8400-e29b-41d4-a716-446655440000';
      mock.onGet(`/api/v1/tasks/${taskId}`).reply(404, {
        message: 'Task not found',
      });

      await expect(client.getTask(taskId)).rejects.toThrow(LoopaiException);
      await expect(client.getTask(taskId)).rejects.toMatchObject({
        statusCode: 404,
      });
    });
  });

  describe('execute', () => {
    it('should execute a task successfully', async () => {
      const executionResponse = {
        id: '789e4567-e89b-12d3-a456-426614174000',
        taskId: '550e8400-e29b-41d4-a716-446655440000',
        version: 1,
        output: 'spam',
        latencyMs: 42.5,
        sampledForValidation: false,
        executedAt: new Date().toISOString(),
      };

      mock.onPost('/api/v1/tasks/execute').reply(200, executionResponse);

      const result = await client.execute({
        taskId: '550e8400-e29b-41d4-a716-446655440000',
        input: { text: 'Buy now!' },
      });

      expect(result.output).toBe('spam');
      expect(result.latencyMs).toBe(42.5);
    });

    it('should throw ValidationException on 400 errors', async () => {
      mock.onPost('/api/v1/tasks/execute').reply(400, {
        message: 'Validation failed',
        errors: { text: ['Field required'] },
      });

      await expect(
        client.execute({
          taskId: '550e8400-e29b-41d4-a716-446655440000',
          input: {},
        })
      ).rejects.toThrow(ValidationException);
    });

    it('should throw ExecutionException on 500 errors', async () => {
      mock.onPost('/api/v1/tasks/execute').reply(500, {
        message: 'Execution failed',
        executionId: '789e4567-e89b-12d3-a456-426614174000',
      });

      await expect(
        client.execute({
          taskId: '550e8400-e29b-41d4-a716-446655440000',
          input: { text: 'Test' },
        })
      ).rejects.toThrow(ExecutionException);
    });
  });

  describe('batchExecute', () => {
    it('should execute batch successfully', async () => {
      const batchResponse = {
        batchId: '890e4567-e89b-12d3-a456-426614174998',
        taskId: '550e8400-e29b-41d4-a716-446655440000',
        version: 1,
        totalItems: 3,
        successCount: 3,
        failureCount: 0,
        totalDurationMs: 150.0,
        avgLatencyMs: 50.0,
        results: [
          {
            id: '0',
            executionId: '111e4567-e89b-12d3-a456-426614174000',
            success: true,
            output: 'spam',
            latencyMs: 45.0,
            sampledForValidation: false,
          },
          {
            id: '1',
            executionId: '222e4567-e89b-12d3-a456-426614174000',
            success: true,
            output: 'not_spam',
            latencyMs: 50.0,
            sampledForValidation: false,
          },
          {
            id: '2',
            executionId: '333e4567-e89b-12d3-a456-426614174000',
            success: true,
            output: 'spam',
            latencyMs: 55.0,
            sampledForValidation: true,
          },
        ],
        startedAt: new Date().toISOString(),
        completedAt: new Date().toISOString(),
      };

      mock.onPost('/api/v1/batch/execute').reply(200, batchResponse);

      const inputs = [
        { text: 'Buy now!' },
        { text: 'Meeting at 2pm' },
        { text: 'Free money!!!' },
      ];

      const result = await client.batchExecute(
        '550e8400-e29b-41d4-a716-446655440000',
        inputs,
        10
      );

      expect(result.totalItems).toBe(3);
      expect(result.successCount).toBe(3);
      expect(result.failureCount).toBe(0);
      expect(result.results).toHaveLength(3);
      expect(result.results[0].output).toBe('spam');
      expect(result.results[2].sampledForValidation).toBe(true);
    });

    it('should handle partial failures', async () => {
      const batchResponse = {
        batchId: '890e4567-e89b-12d3-a456-426614174998',
        taskId: '550e8400-e29b-41d4-a716-446655440000',
        version: 1,
        totalItems: 2,
        successCount: 1,
        failureCount: 1,
        totalDurationMs: 100.0,
        avgLatencyMs: 50.0,
        results: [
          {
            id: '0',
            executionId: '111e4567-e89b-12d3-a456-426614174000',
            success: true,
            output: 'spam',
            latencyMs: 45.0,
            sampledForValidation: false,
          },
          {
            id: '1',
            executionId: '222e4567-e89b-12d3-a456-426614174000',
            success: false,
            errorMessage: 'Execution timeout',
            latencyMs: 55.0,
            sampledForValidation: false,
          },
        ],
        startedAt: new Date().toISOString(),
        completedAt: new Date().toISOString(),
      };

      mock.onPost('/api/v1/batch/execute').reply(200, batchResponse);

      const inputs = [{ text: 'Email 1' }, { text: 'Email 2' }];

      const result = await client.batchExecute(
        '550e8400-e29b-41d4-a716-446655440000',
        inputs
      );

      expect(result.successCount).toBe(1);
      expect(result.failureCount).toBe(1);
      expect(result.results[0].success).toBe(true);
      expect(result.results[1].success).toBe(false);
      expect(result.results[1].errorMessage).toBe('Execution timeout');
    });
  });

  describe('batchExecuteAdvanced', () => {
    it('should execute advanced batch with custom options', async () => {
      const batchResponse = {
        batchId: '890e4567-e89b-12d3-a456-426614174998',
        taskId: '550e8400-e29b-41d4-a716-446655440000',
        version: 1,
        totalItems: 2,
        successCount: 2,
        failureCount: 0,
        totalDurationMs: 100.0,
        avgLatencyMs: 50.0,
        results: [
          {
            id: 'email-001',
            executionId: '111e4567-e89b-12d3-a456-426614174000',
            success: true,
            output: 'spam',
            latencyMs: 45.0,
            sampledForValidation: false,
          },
          {
            id: 'email-002',
            executionId: '222e4567-e89b-12d3-a456-426614174000',
            success: true,
            output: 'not_spam',
            latencyMs: 55.0,
            sampledForValidation: true,
          },
        ],
        startedAt: new Date().toISOString(),
        completedAt: new Date().toISOString(),
      };

      mock.onPost('/api/v1/batch/execute').reply(200, batchResponse);

      const result = await client.batchExecuteAdvanced({
        taskId: '550e8400-e29b-41d4-a716-446655440000',
        items: [
          { id: 'email-001', input: { text: 'Spam email' } },
          {
            id: 'email-002',
            input: { text: 'Legitimate email' },
            forceValidation: true,
          },
        ],
        maxConcurrency: 5,
        stopOnFirstError: false,
        timeoutMs: 30000,
      });

      expect(result.results).toHaveLength(2);
      expect(result.results[0].id).toBe('email-001');
      expect(result.results[1].id).toBe('email-002');
      expect(result.results[1].sampledForValidation).toBe(true);
    });
  });
});
