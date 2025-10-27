/**
 * Loopai TypeScript/JavaScript client implementation
 */

import axios, {
  AxiosInstance,
  AxiosError,
  AxiosRequestConfig,
  AxiosResponse,
} from 'axios';
import {
  LoopaiClientOptions,
  Task,
  CreateTaskRequest,
  ExecuteRequest,
  ExecutionResult,
  BatchExecuteRequest,
  BatchExecuteResponse,
  HealthResponse,
} from './types';
import {
  LoopaiException,
  ValidationException,
  ExecutionException,
  ConnectionException,
} from './exceptions';

/**
 * Official TypeScript/JavaScript client for Loopai API
 *
 * @example
 * ```typescript
 * const client = new LoopaiClient({ baseUrl: 'http://localhost:8080' });
 *
 * const task = await client.createTask({
 *   name: 'spam-classifier',
 *   description: 'Classify emails as spam or not spam',
 *   inputSchema: { type: 'object', properties: { text: { type: 'string' } } },
 *   outputSchema: { type: 'string', enum: ['spam', 'not_spam'] }
 * });
 *
 * const result = await client.execute({
 *   taskId: task.id,
 *   input: { text: 'Buy now for free!' }
 * });
 * ```
 */
export class LoopaiClient {
  private readonly client: AxiosInstance;
  private readonly maxRetries: number;

  /**
   * Create a new Loopai client
   *
   * @param options - Client configuration options
   */
  constructor(options: LoopaiClientOptions = {}) {
    const {
      baseUrl = 'http://localhost:8080',
      apiKey,
      timeout = 30000,
      maxRetries = 3,
    } = options;

    this.maxRetries = maxRetries;

    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
      'User-Agent': 'loopai-typescript/0.1.0',
    };

    if (apiKey) {
      headers['Authorization'] = `Bearer ${apiKey}`;
    }

    this.client = axios.create({
      baseURL: baseUrl.replace(/\/$/, ''),
      timeout,
      headers,
    });
  }

  /**
   * Create a new task
   *
   * @param request - Task creation request
   * @returns Created task
   *
   * @example
   * ```typescript
   * const task = await client.createTask({
   *   name: 'spam-classifier',
   *   description: 'Classify emails',
   *   inputSchema: { type: 'object', properties: { text: { type: 'string' } } },
   *   outputSchema: { type: 'string', enum: ['spam', 'not_spam'] },
   *   accuracyTarget: 0.95,
   *   latencyTargetMs: 50
   * });
   * ```
   */
  async createTask(request: CreateTaskRequest): Promise<Task> {
    const response = await this.post<Task>('/api/v1/tasks', {
      name: request.name,
      description: request.description,
      input_schema: request.inputSchema,
      output_schema: request.outputSchema,
      examples: request.examples || [],
      accuracy_target: request.accuracyTarget ?? 0.9,
      latency_target_ms: request.latencyTargetMs ?? 10,
      sampling_rate: request.samplingRate ?? 0.1,
    });

    return response;
  }

  /**
   * Get task by ID
   *
   * @param taskId - Task identifier
   * @returns Task details
   *
   * @example
   * ```typescript
   * const task = await client.getTask('550e8400-e29b-41d4-a716-446655440000');
   * ```
   */
  async getTask(taskId: string): Promise<Task> {
    return await this.get<Task>(`/api/v1/tasks/${taskId}`);
  }

  /**
   * Execute a task with given input
   *
   * @param request - Execution request
   * @returns Execution result
   *
   * @example
   * ```typescript
   * const result = await client.execute({
   *   taskId: task.id,
   *   input: { text: 'Buy now for free money!' }
   * });
   * console.log(result.output); // 'spam'
   * ```
   */
  async execute(request: ExecuteRequest): Promise<ExecutionResult> {
    const response = await this.post<ExecutionResult>('/api/v1/tasks/execute', {
      task_id: request.taskId,
      input: request.input,
      version: request.version,
      timeout_ms: request.timeoutMs,
      force_validation: request.forceValidation ?? false,
    });

    return response;
  }

  /**
   * Execute multiple inputs in batch (simplified interface)
   *
   * @param taskId - Task identifier
   * @param inputs - Array of input data
   * @param maxConcurrency - Maximum concurrent executions (default: 10)
   * @returns Batch execution response
   *
   * @example
   * ```typescript
   * const inputs = [
   *   { text: 'Email 1' },
   *   { text: 'Email 2' },
   *   { text: 'Email 3' }
   * ];
   * const result = await client.batchExecute(task.id, inputs, 10);
   * console.log(`Success: ${result.successCount}/${result.totalItems}`);
   * ```
   */
  async batchExecute(
    taskId: string,
    inputs: Array<Record<string, unknown>>,
    maxConcurrency: number = 10
  ): Promise<BatchExecuteResponse> {
    const items = inputs.map((input, index) => ({
      id: index.toString(),
      input,
      forceValidation: false,
    }));

    return await this.batchExecuteAdvanced({
      taskId,
      items,
      maxConcurrency,
    });
  }

  /**
   * Execute batch with advanced options
   *
   * @param request - Batch execute request
   * @returns Batch execution response
   *
   * @example
   * ```typescript
   * const result = await client.batchExecuteAdvanced({
   *   taskId: task.id,
   *   items: [
   *     { id: 'email-001', input: { text: 'Email 1' }, forceValidation: false },
   *     { id: 'email-002', input: { text: 'Email 2' }, forceValidation: true }
   *   ],
   *   maxConcurrency: 5,
   *   stopOnFirstError: false,
   *   timeoutMs: 30000
   * });
   * ```
   */
  async batchExecuteAdvanced(
    request: BatchExecuteRequest
  ): Promise<BatchExecuteResponse> {
    const response = await this.post<BatchExecuteResponse>('/api/v1/batch/execute', {
      task_id: request.taskId,
      items: request.items.map((item) => ({
        id: item.id,
        input: item.input,
        force_validation: item.forceValidation ?? false,
      })),
      version: request.version,
      max_concurrency: request.maxConcurrency ?? 10,
      stop_on_first_error: request.stopOnFirstError ?? false,
      timeout_ms: request.timeoutMs,
    });

    return response;
  }

  /**
   * Get API health status
   *
   * @returns Health check response
   *
   * @example
   * ```typescript
   * const health = await client.getHealth();
   * console.log(`API Status: ${health.status} (v${health.version})`);
   * ```
   */
  async getHealth(): Promise<HealthResponse> {
    return await this.get<HealthResponse>('/health');
  }

  /**
   * Execute GET request with retry logic
   */
  private async get<T>(path: string): Promise<T> {
    return await this.request<T>('GET', path);
  }

  /**
   * Execute POST request with retry logic
   */
  private async post<T>(path: string, data?: unknown): Promise<T> {
    return await this.request<T>('POST', path, data);
  }

  /**
   * Execute HTTP request with retry logic and error handling
   */
  private async request<T>(
    method: string,
    path: string,
    data?: unknown
  ): Promise<T> {
    let lastError: Error | undefined;

    for (let attempt = 0; attempt <= this.maxRetries; attempt++) {
      try {
        const config: AxiosRequestConfig = { method, url: path };
        if (data) {
          config.data = data;
        }

        const response: AxiosResponse<T> = await this.client.request(config);
        return response.data;
      } catch (error) {
        lastError = error as Error;

        if (axios.isAxiosError(error)) {
          const axiosError = error as AxiosError;

          // Don't retry on 4xx errors (except 408 Request Timeout and 429 Too Many Requests)
          if (
            axiosError.response &&
            axiosError.response.status >= 400 &&
            axiosError.response.status < 500 &&
            axiosError.response.status !== 408 &&
            axiosError.response.status !== 429
          ) {
            throw this.handleError(axiosError);
          }

          // Retry on 5xx errors and transient failures
          if (attempt < this.maxRetries) {
            const delay = this.calculateRetryDelay(attempt);
            await this.sleep(delay);
            continue;
          }

          // Give up after max retries
          throw this.handleError(axiosError);
        }

        // Non-Axios error (network error, etc.)
        if (attempt < this.maxRetries) {
          const delay = this.calculateRetryDelay(attempt);
          await this.sleep(delay);
          continue;
        }

        throw new ConnectionException(
          `Failed to connect to Loopai API after ${this.maxRetries + 1} attempts`,
          lastError
        );
      }
    }

    // Should not reach here, but handle gracefully
    throw new ConnectionException(
      'Request failed after all retries',
      lastError
    );
  }

  /**
   * Handle Axios error and convert to appropriate exception
   */
  private handleError(error: AxiosError): LoopaiException {
    const response = error.response;

    if (!response) {
      return new ConnectionException(
        error.message || 'Network error',
        error
      );
    }

    const data = response.data as any;
    const message = data?.message || response.statusText || 'Unknown error';
    const statusCode = response.status;

    if (statusCode === 400) {
      const errors = data?.errors || {};
      return new ValidationException(message, errors, statusCode);
    }

    if (statusCode === 404) {
      return new LoopaiException(
        `Resource not found: ${message}`,
        statusCode,
        data
      );
    }

    if (statusCode === 500) {
      const executionId = data?.executionId;
      return new ExecutionException(message, executionId, statusCode);
    }

    return new LoopaiException(message, statusCode, data);
  }

  /**
   * Calculate retry delay with exponential backoff
   */
  private calculateRetryDelay(attempt: number): number {
    return Math.min(500 * Math.pow(2, attempt), 10000); // Max 10 seconds
  }

  /**
   * Sleep for specified milliseconds
   */
  private sleep(ms: number): Promise<void> {
    return new Promise((resolve) => setTimeout(resolve, ms));
  }
}
