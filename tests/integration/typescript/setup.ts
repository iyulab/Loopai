/**
 * Jest global setup for integration tests
 * Waits for API server to be ready before running tests
 */
import axios from 'axios';
import * as fs from 'fs';
import * as path from 'path';

interface TestConfig {
  baseUrl: string;
  timeout: number;
}

async function waitForApi(baseUrl: string, maxAttempts: number = 30): Promise<void> {
  for (let attempt = 0; attempt < maxAttempts; attempt++) {
    try {
      const response = await axios.get(`${baseUrl}/health`, { timeout: 5000 });
      if (response.status === 200) {
        console.log('API server is ready');
        return;
      }
    } catch (error) {
      if (attempt === maxAttempts - 1) {
        throw new Error(`API server not available after ${maxAttempts} attempts`);
      }
      await new Promise(resolve => setTimeout(resolve, 1000));
    }
  }
}

export default async function globalSetup() {
  // Load test config
  const configPath = path.join(__dirname, '..', 'test-config.json');
  const config: TestConfig = JSON.parse(fs.readFileSync(configPath, 'utf-8'));

  console.log(`Waiting for API server at ${config.baseUrl}...`);
  await waitForApi(config.baseUrl);
}
