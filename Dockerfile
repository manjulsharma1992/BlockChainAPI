# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app


# Copy the rest of the app and build
COPY . .
RUN dotnet publish -c Release -o out

# Stage 2: Run
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .

# Set environment variable
ENV ASPNETCORE_URLS=http://+:5000

# Expose port
EXPOSE 5000

# Start app
ENTRYPOINT ["dotnet", "MultiChainAPI.dll"]
