FROM mcr.microsoft.com/dotnet/runtime:9.0

# Install GCC, G++, Make, and C standard library development files
RUN apt-get update && \
    apt-get install -y --no-install-recommends gcc g++ make libc6-dev && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Set working directory
WORKDIR /app

# Set environment variables
ENV DOTNET_RUNNING_IN_CONTAINER=true