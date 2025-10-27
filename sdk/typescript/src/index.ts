/**
 * Loopai TypeScript/JavaScript SDK
 *
 * Official client library for Loopai - Human-in-the-Loop AI Self-Improvement Framework
 *
 * @packageDocumentation
 */

export { LoopaiClient } from './client';
export {
  LoopaiException,
  ValidationException,
  ExecutionException,
  ConnectionException,
} from './exceptions';
export type {
  LoopaiClientOptions,
  Task,
  CreateTaskRequest,
  ExecuteRequest,
  ExecutionResult,
  BatchExecuteItem,
  BatchExecuteRequest,
  BatchExecuteResult,
  BatchExecuteResponse,
  HealthResponse,
} from './types';
