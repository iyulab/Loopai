/**
 * Type definitions for Loopai SDK
 */

/**
 * Task representation
 */
export interface Task {
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

/**
 * Execution result
 */
export interface ExecutionResult {
  id: string;
  taskId: string;
  version: number;
  output: unknown;
  latencyMs: number;
  sampledForValidation: boolean;
  executedAt: string;
}

/**
 * Batch execute item
 */
export interface BatchExecuteItem {
  id: string;
  input: Record<string, unknown>;
  forceValidation?: boolean;
}

/**
 * Batch execute request
 */
export interface BatchExecuteRequest {
  taskId: string;
  items: BatchExecuteItem[];
  version?: number;
  maxConcurrency?: number;
  stopOnFirstError?: boolean;
  timeoutMs?: number;
}

/**
 * Batch execute result for a single item
 */
export interface BatchExecuteResult {
  id: string;
  executionId: string;
  success: boolean;
  output?: unknown;
  errorMessage?: string;
  latencyMs: number;
  sampledForValidation: boolean;
}

/**
 * Batch execute response
 */
export interface BatchExecuteResponse {
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

/**
 * Health check response
 */
export interface HealthResponse {
  status: string;
  version: string;
  timestamp: string;
}

/**
 * Client configuration options
 */
export interface LoopaiClientOptions {
  /** Base URL of the Loopai API */
  baseUrl?: string;
  /** Optional API key for authentication */
  apiKey?: string;
  /** Request timeout in milliseconds */
  timeout?: number;
  /** Maximum number of retry attempts */
  maxRetries?: number;
}

/**
 * Task creation request
 */
export interface CreateTaskRequest {
  name: string;
  description: string;
  inputSchema: Record<string, unknown>;
  outputSchema: Record<string, unknown>;
  examples?: Array<{ input: unknown; output: unknown }>;
  accuracyTarget?: number;
  latencyTargetMs?: number;
  samplingRate?: number;
}

/**
 * Task execution request
 */
export interface ExecuteRequest {
  taskId: string;
  input: Record<string, unknown>;
  version?: number;
  timeoutMs?: number;
  forceValidation?: boolean;
}
