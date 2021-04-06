FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY . ./
RUN dotnet restore
RUN dotnet publish -c Release -o Out

# Build runtime image
FROM gustavogmoraes/mega.puppeteersharp-aspnetcore-3.1-base
WORKDIR /app
COPY --from=build-env /app/Output .
ENTRYPOINT ["dotnet", "Mega.WhatsAppAutomator.Api.dll"]