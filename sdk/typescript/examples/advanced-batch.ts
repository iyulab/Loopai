/**
 * Advanced batch execution example with custom options
 */

import { LoopaiClient } from '../src';

async function main() {
  const client = new LoopaiClient({
    baseUrl: 'http://localhost:8080',
  });

  try {
    // Assume task already created
    const taskId = '550e8400-e29b-41d4-a716-446655440000'; // Replace with actual task ID

    // Execute batch with advanced options
    const result = await client.batchExecuteAdvanced({
      taskId,
      items: [
        {
          id: 'email-001',
          input: { text: 'Congratulations! You won $1,000,000!' },
          forceValidation: false,
        },
        {
          id: 'email-002',
          input: { text: 'Hi, can we schedule a meeting tomorrow?' },
          forceValidation: true, // Force validation for this specific item
        },
        {
          id: 'email-003',
          input: { text: 'URGENT: Click here to claim your prize NOW!' },
          forceValidation: false,
        },
      ],
      maxConcurrency: 5, // Limit concurrent executions
      stopOnFirstError: false, // Continue even if some items fail
      timeoutMs: 30000, // 30 second timeout
    });

    console.log(`Batch ID: ${result.batchId}`);
    console.log(`Total items: ${result.totalItems}`);
    console.log(`Success: ${result.successCount}`);
    console.log(`Failed: ${result.failureCount}`);
    console.log(`Duration: ${result.totalDurationMs.toFixed(2)}ms`);
    console.log(`Started: ${result.startedAt}`);
    console.log(`Completed: ${result.completedAt}`);

    console.log('\nResults:');
    for (const item of result.results) {
      console.log(`\nItem: ${item.id}`);
      console.log(`  Success: ${item.success}`);
      if (item.success) {
        console.log(`  Output: ${item.output}`);
        console.log(`  Latency: ${item.latencyMs.toFixed(2)}ms`);
        console.log(`  Sampled for validation: ${item.sampledForValidation}`);
      } else {
        console.log(`  Error: ${item.errorMessage}`);
      }
    }
  } catch (error) {
    console.error('Error:', error);
  }
}

main();
