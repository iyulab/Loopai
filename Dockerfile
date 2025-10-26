# Loopai Edge Runtime - Production Dockerfile
# Multi-stage build for minimal image size

# Stage 1: Build dependencies
FROM python:3.9-slim AS builder

WORKDIR /build

# Install build dependencies
RUN apt-get update && apt-get install -y --no-install-recommends \
    gcc \
    && rm -rf /var/lib/apt/lists/*

# Copy requirements and install dependencies
COPY requirements.txt .
RUN pip install --no-cache-dir --user -r requirements.txt

# Stage 2: Production runtime
FROM python:3.9-slim

# Create non-root user
RUN useradd -m -u 1000 loopai && \
    mkdir -p /loopai-data && \
    chown -R loopai:loopai /loopai-data

# Set working directory
WORKDIR /app

# Copy Python dependencies from builder
COPY --from=builder /root/.local /home/loopai/.local

# Copy application code
COPY --chown=loopai:loopai src/ ./src/

# Set environment variables
ENV PYTHONUNBUFFERED=1 \
    PYTHONDONTWRITEBYTECODE=1 \
    PATH=/home/loopai/.local/bin:$PATH \
    LOOPAI_DATA_DIR=/loopai-data

# Switch to non-root user
USER loopai

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD python -c "import httpx; httpx.get('http://localhost:8080/health', timeout=5.0)"

# Run the application
CMD ["python", "-m", "uvicorn", "loopai.runtime.main:app", "--host", "0.0.0.0", "--port", "8080"]
