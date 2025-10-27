module.exports = {
  preset: 'ts-jest',
  testEnvironment: 'node',
  roots: ['<rootDir>'],
  testMatch: ['**/*.test.ts'],
  collectCoverageFrom: [
    '../../../sdk/typescript/src/**/*.ts',
    '!../../../sdk/typescript/src/**/*.d.ts',
  ],
  moduleNameMapper: {
    '^@loopai/sdk$': '<rootDir>/../../../sdk/typescript/src',
  },
  testTimeout: 30000,
  globalSetup: '<rootDir>/setup.ts',
};
