/**
 * Exception classes for Loopai SDK
 */

/**
 * Base exception for all Loopai SDK errors
 */
export class LoopaiException extends Error {
  statusCode?: number;
  response?: unknown;

  constructor(message: string, statusCode?: number, response?: unknown) {
    super(message);
    this.name = 'LoopaiException';
    this.statusCode = statusCode;
    this.response = response;
    Object.setPrototypeOf(this, LoopaiException.prototype);
  }
}

/**
 * Exception raised when input or output validation fails
 */
export class ValidationException extends LoopaiException {
  errors: Record<string, string[]>;

  constructor(
    message: string,
    errors: Record<string, string[]> = {},
    statusCode: number = 400
  ) {
    super(message, statusCode);
    this.name = 'ValidationException';
    this.errors = errors;
    Object.setPrototypeOf(this, ValidationException.prototype);
  }
}

/**
 * Exception raised when program execution fails
 */
export class ExecutionException extends LoopaiException {
  executionId?: string;

  constructor(message: string, executionId?: string, statusCode: number = 500) {
    super(message, statusCode);
    this.name = 'ExecutionException';
    this.executionId = executionId;
    Object.setPrototypeOf(this, ExecutionException.prototype);
  }
}

/**
 * Exception raised when connection to Loopai API fails
 */
export class ConnectionException extends LoopaiException {
  originalError?: Error;

  constructor(message: string, originalError?: Error) {
    super(message);
    this.name = 'ConnectionException';
    this.originalError = originalError;
    Object.setPrototypeOf(this, ConnectionException.prototype);
  }
}
