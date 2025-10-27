/**
 * Basic usage example for Loopai TypeScript SDK
 */

import { LoopaiClient } from '../src';

async function main() {
  // Initialize client
  const client = new LoopaiClient({
    baseUrl: 'http://localhost:8080',
  });

  try {
    // Check health
    const health = await client.getHealth();
    console.log(`API Status: ${health.status} (v${health.version})`);

    // Create a task
    const task = await client.createTask({
      name: 'spam-classifier',
      description: 'Classify emails as spam or not spam',
      inputSchema: {
        type: 'object',
        properties: {
          text: { type: 'string' },
        },
        required: ['text'],
      },
      outputSchema: {
        type: 'string',
        enum: ['spam', 'not_spam'],
      },
      accuracyTarget: 0.95,
      latencyTargetMs: 50,
    });
    console.log(`\nCreated task: ${task.name} (${task.id})`);

    // Execute single input
    const result = await client.execute({
      taskId: task.id,
      input: { text: 'Buy now for free money!' },
    });
    console.log(
      `\nClassification: ${result.output} (latency: ${result.latencyMs}ms)`
    );

    // Batch execute
    const emails = [
      { text: 'Buy now for free money!' },
      { text: 'Meeting tomorrow at 2pm' },
      { text: 'URGENT: Click here NOW!!!' },
      { text: 'Can we schedule a call next week?' },
    ];

    const batchResult = await client.batchExecute(task.id, emails, 10);

    console.log('\nBatch Results:');
    console.log(`  Total: ${batchResult.totalItems}`);
    console.log(`  Success: ${batchResult.successCount}`);
    console.log(`  Failed: ${batchResult.failureCount}`);
    console.log(`  Avg latency: ${batchResult.avgLatencyMs.toFixed(2)}ms`);
    console.log(`  Total duration: ${batchResult.totalDurationMs.toFixed(2)}ms`);

    console.log('\nIndividual Results:');
    for (const item of batchResult.results) {
      if (item.success) {
        console.log(
          `  ${item.id}: ${item.output} (latency: ${item.latencyMs.toFixed(2)}ms, ` +
            `sampled: ${item.sampledForValidation})`
        );
      } else {
        console.log(`  ${item.id}: FAILED - ${item.errorMessage}`);
      }
    }
  } catch (error) {
    console.error('Error:', error);
  }
}

main();
