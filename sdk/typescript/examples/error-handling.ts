/**
 * Error handling example for Loopai TypeScript SDK
 */

import {
  LoopaiClient,
  LoopaiException,
  ValidationException,
  ExecutionException,
  ConnectionException,
} from '../src';

async function main() {
  const client = new LoopaiClient({
    baseUrl: 'http://localhost:8080',
    timeout: 30000,
    maxRetries: 3,
  });

  try {
    // Example 1: Validation error (invalid input)
    console.log('Example 1: Validation Error');
    const taskId = '550e8400-e29b-41d4-a716-446655440000'; // Assume this exists
    try {
      await client.execute({
        taskId,
        input: {}, // Missing required field
      });
    } catch (error) {
      if (error instanceof ValidationException) {
        console.log(`Validation failed: ${error.message}`);
        console.log(`Field errors:`, error.errors);
        console.log(`Status code: ${error.statusCode}`);
      }
    }

    // Example 2: Task not found (404)
    console.log('\nExample 2: Not Found Error');
    try {
      await client.getTask('00000000-0000-0000-0000-000000000000'); // Non-existent task
    } catch (error) {
      if (error instanceof LoopaiException && error.statusCode === 404) {
        console.log(`Task not found: ${error.message}`);
      }
    }

    // Example 3: Execution error
    console.log('\nExample 3: Execution Error');
    try {
      await client.execute({
        taskId,
        input: { text: 'Test' },
      });
    } catch (error) {
      if (error instanceof ExecutionException) {
        console.log(`Execution failed: ${error.message}`);
        console.log(`Execution ID: ${error.executionId}`);
        console.log(`Status code: ${error.statusCode}`);
      }
    }

    // Example 4: Connection error
    console.log('\nExample 4: Connection Error');
    const badClient = new LoopaiClient({
      baseUrl: 'http://invalid-host:9999',
      maxRetries: 1,
    });
    try {
      await badClient.getHealth();
    } catch (error) {
      if (error instanceof ConnectionException) {
        console.log(`Connection failed: ${error.message}`);
        console.log(`Original error:`, error.originalError?.message);
      }
    }

    // Example 5: Generic error handling
    console.log('\nExample 5: Generic Error Handling');
    try {
      await client.execute({
        taskId,
        input: { text: 'Test' },
      });
    } catch (error) {
      if (error instanceof ValidationException) {
        // Handle validation errors
        console.log(`Input validation failed:`, error.errors);
      } else if (error instanceof ExecutionException) {
        // Handle execution errors
        console.log(`Execution failed: ${error.executionId}`);
      } else if (error instanceof LoopaiException) {
        // Handle other API errors
        console.log(`API error: ${error.message}`);
      } else {
        // Handle unexpected errors
        console.log(`Unexpected error:`, error);
      }
    }
  } catch (error) {
    console.error('Fatal error:', error);
  }
}

main();
