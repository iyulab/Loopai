# Loopai TypeScript/JavaScript SDK

Official TypeScript/JavaScript client library for [Loopai](https://github.com/iyulab/loopai) - Human-in-the-Loop AI Self-Improvement Framework.

## Features

- ✅ **TypeScript Support**: Full type definitions for excellent IDE support
- ✅ **Promise-Based**: Modern async/await API
- ✅ **Automatic Retry**: Exponential backoff for transient failures
- ✅ **Batch Execution**: Process multiple inputs with concurrency control
- ✅ **Type-Safe**: Complete TypeScript interfaces and types
- ✅ **Error Handling**: Specific exception classes for different error types
- ✅ **Node.js & Browser**: Works in both environments

## Installation

### npm

```bash
npm install @loopai/sdk
```

### yarn

```bash
yarn add @loopai/sdk
```

### pnpm

```bash
pnpm add @loopai/sdk
```

## Quick Start

### TypeScript

```typescript
import { LoopaiClient } from '@loopai/sdk';

const client = new LoopaiClient({
  baseUrl: 'http://localhost:8080',
});

// Create a task
const task = await client.createTask({
  name: 'spam-classifier',
  description: 'Classify emails as spam or not spam',
  inputSchema: {
    type: 'object',
    properties: { text: { type: 'string' } },
    required: ['text']
  },
  outputSchema: {
    type: 'string',
    enum: ['spam', 'not_spam']
  },
  accuracyTarget: 0.95,
  latencyTargetMs: 50
});

// Execute
const result = await client.execute({
  taskId: task.id,
  input: { text: 'Buy now for free money!' }
});
console.log(result.output); // 'spam'
```

### JavaScript (CommonJS)

```javascript
const { LoopaiClient } = require('@loopai/sdk');

const client = new LoopaiClient({
  baseUrl: 'http://localhost:8080',
});

async function main() {
  const task = await client.createTask({
    name: 'spam-classifier',
    description: 'Classify emails',
    inputSchema: { type: 'object', properties: { text: { type: 'string' } } },
    outputSchema: { type: 'string', enum: ['spam', 'not_spam'] }
  });

  const result = await client.execute({
    taskId: task.id,
    input: { text: 'Buy now!' }
  });
  console.log(result.output);
}

main();
```

## Usage

### Creating a Task

```typescript
const task = await client.createTask({
  name: 'spam-classifier',
  description: 'Classify emails as spam or not spam',
  inputSchema: {
    type: 'object',
    properties: { text: { type: 'string' } },
    required: ['text']
  },
  outputSchema: {
    type: 'string',
    enum: ['spam', 'not_spam']
  },
  accuracyTarget: 0.95,
  latencyTargetMs: 50,
  samplingRate: 0.1
});
```

### Executing a Task

```typescript
const result = await client.execute({
  taskId: task.id,
  input: { text: 'Buy now for free money!' },
  forceValidation: false
});

console.log(`Result: ${result.output}`);
console.log(`Latency: ${result.latencyMs}ms`);
console.log(`Sampled: ${result.sampledForValidation}`);
```

### Batch Execution

#### Simple Batch

```typescript
const emails = [
  { text: 'Buy now for free money!' },
  { text: 'Meeting tomorrow at 2pm' },
  { text: 'URGENT: Click here NOW!!!' }
];

const result = await client.batchExecute(task.id, emails, 10);

console.log(`Success: ${result.successCount}/${result.totalItems}`);
console.log(`Average latency: ${result.avgLatencyMs}ms`);

for (const item of result.results) {
  if (item.success) {
    console.log(`${item.id}: ${item.output}`);
  } else {
    console.log(`${item.id}: FAILED - ${item.errorMessage}`);
  }
}
```

#### Advanced Batch

```typescript
const result = await client.batchExecuteAdvanced({
  taskId: task.id,
  items: [
    {
      id: 'email-001',
      input: { text: 'Spam email' },
      forceValidation: false
    },
    {
      id: 'email-002',
      input: { text: 'Legitimate email' },
      forceValidation: true  // Force validation for this item
    }
  ],
  maxConcurrency: 5,
  stopOnFirstError: false,
  timeoutMs: 30000
});
```

### Error Handling

```typescript
import {
  LoopaiException,
  ValidationException,
  ExecutionException,
  ConnectionException
} from '@loopai/sdk';

try {
  const result = await client.execute({
    taskId: task.id,
    input: { text: 'Test' }
  });
} catch (error) {
  if (error instanceof ValidationException) {
    // Handle input validation errors (400)
    console.log('Validation failed:', error.errors);
  } else if (error instanceof ExecutionException) {
    // Handle execution failures (500)
    console.log('Execution failed:', error.executionId);
  } else if (error instanceof ConnectionException) {
    // Handle connection errors
    console.log('Connection failed:', error.originalError);
  } else if (error instanceof LoopaiException) {
    // Handle general API errors
    console.log('API error:', error.message);
  }
}
```

### Health Check

```typescript
const health = await client.getHealth();
console.log(`API Status: ${health.status} (v${health.version})`);
```

## API Reference

### LoopaiClient

#### Constructor

```typescript
new LoopaiClient(options?: LoopaiClientOptions)
```

**Options:**
- `baseUrl?: string` - API base URL (default: `http://localhost:8080`)
- `apiKey?: string` - Optional API key for authentication
- `timeout?: number` - Request timeout in milliseconds (default: `30000`)
- `maxRetries?: number` - Maximum retry attempts (default: `3`)

#### Methods

##### `createTask(request: CreateTaskRequest): Promise<Task>`

Create a new task.

##### `getTask(taskId: string): Promise<Task>`

Retrieve task by ID.

##### `execute(request: ExecuteRequest): Promise<ExecutionResult>`

Execute a task with input.

##### `batchExecute(taskId: string, inputs: Array<Record<string, unknown>>, maxConcurrency?: number): Promise<BatchExecuteResponse>`

Execute multiple inputs in batch (simplified interface).

##### `batchExecuteAdvanced(request: BatchExecuteRequest): Promise<BatchExecuteResponse>`

Execute batch with advanced options.

##### `getHealth(): Promise<HealthResponse>`

Get API health status.

## Types

### Task

```typescript
interface Task {
  id: string;
  name: string;
  description: string;
  inputSchema: Record<string, unknown>;
  outputSchema: Record<string, unknown>;
  accuracyTarget: number;
  latencyTargetMs: number;
  samplingRate: number;
  createdAt: string;
}
```

### ExecutionResult

```typescript
interface ExecutionResult {
  id: string;
  taskId: string;
  version: number;
  output: unknown;
  latencyMs: number;
  sampledForValidation: boolean;
  executedAt: string;
}
```

### BatchExecuteResponse

```typescript
interface BatchExecuteResponse {
  batchId: string;
  taskId: string;
  version: number;
  totalItems: number;
  successCount: number;
  failureCount: number;
  totalDurationMs: number;
  avgLatencyMs: number;
  results: BatchExecuteResult[];
  startedAt: string;
  completedAt: string;
}
```

## Exceptions

- `LoopaiException` - Base exception for all SDK errors
- `ValidationException` - Input or output validation failed (HTTP 400)
- `ExecutionException` - Program execution failed (HTTP 500)
- `ConnectionException` - Connection to API failed

## Development

### Setup

```bash
cd sdk/typescript
npm install
```

### Build

```bash
npm run build
```

### Test

```bash
# Run all tests
npm test

# Run tests in watch mode
npm run test:watch

# Run tests with coverage
npm run test:coverage
```

### Lint

```bash
npm run lint
```

### Format

```bash
npm run format
```

### Type Check

```bash
npm run typecheck
```

## Examples

See the [examples](examples/) directory for complete examples:

- [`basic-usage.ts`](examples/basic-usage.ts) - Basic task creation and execution
- [`advanced-batch.ts`](examples/advanced-batch.ts) - Advanced batch execution with custom options
- [`error-handling.ts`](examples/error-handling.ts) - Comprehensive error handling patterns

## Best Practices

### 1. Use TypeScript

```typescript
// ✅ Good - Type-safe code
import { LoopaiClient, Task, ExecutionResult } from '@loopai/sdk';

const client = new LoopaiClient();
const task: Task = await client.createTask({...});
const result: ExecutionResult = await client.execute({...});

// ❌ Bad - No type safety
const client = new LoopaiClient();
const task = await client.createTask({...});
const result = await client.execute({...});
```

### 2. Handle Specific Exceptions

```typescript
// ✅ Good - Handle specific error types
try {
  const result = await client.execute({...});
} catch (error) {
  if (error instanceof ValidationException) {
    // Handle validation errors
  } else if (error instanceof ExecutionException) {
    // Handle execution errors
  }
}

// ❌ Bad - Generic catch
try {
  const result = await client.execute({...});
} catch (error) {
  console.error(error); // Too broad
}
```

### 3. Configure Appropriate Timeout

```typescript
// ✅ Good - Appropriate timeout for long-running operations
const client = new LoopaiClient({
  timeout: 60000,  // 60 seconds
  maxRetries: 5
});

// ❌ Bad - Too short timeout
const client = new LoopaiClient({
  timeout: 1000  // May timeout prematurely
});
```

### 4. Use Batch for Multiple Items

```typescript
// ✅ Good - Batch execution with concurrency control
const result = await client.batchExecute(taskId, emails, 10);

// ❌ Bad - Sequential execution
for (const email of emails) {
  await client.execute({ taskId, input: email });
}
```

## Requirements

- Node.js >= 16.0.0
- TypeScript >= 5.0.0 (for TypeScript users)

## License

MIT License - see [LICENSE](../../LICENSE) for details.

## Support

- **GitHub Issues**: [github.com/iyulab/loopai/issues](https://github.com/iyulab/loopai/issues)
- **Documentation**: [github.com/iyulab/loopai](https://github.com/iyulab/loopai)

## Contributing

Contributions are welcome! Please read our contributing guidelines before submitting pull requests.
