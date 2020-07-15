FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
ENV ASPNETCORE_URLS=http://+:80;https://+:443;
ENV PUPPETEER_EXECUTABLE_PATH=/usr/bin/google-chrome-unstable
ENV DOTNET_RUNNING_IN_CONTAINER=true

FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build

WORKDIR /src
COPY ["Mega.WhatsAppAutomator.Api/Mega.WhatsAppAutomator.Api.csproj", "Mega.WhatsAppAutomator.Api/"]
RUN dotnet restore "Mega.WhatsAppAutomator.Api/Mega.WhatsAppAutomator.Api.csproj"
COPY . .

WORKDIR "/src/Mega.WhatsAppAutomator.Api"
RUN dotnet build -c Release -o /app/build

WORKDIR /src
COPY Output/free.gsoftware.client.certificate.with.password.pfx free.gsoftware.client.certificate.with.password.pfx

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# --------------------------- Setting up Puppeteer dependencies ---------------------------------
RUN apt-get update && apt-get install -y apt-transport-https
RUN apt-get update && apt-get -f install && apt-get -y install wget gnupg2 apt-utils -y
RUN wget -q -O - https://dl-ssl.google.com/linux/linux_signing_key.pub | apt-key add -
RUN sh -c "echo 'deb http://dl.google.com/linux/chrome/deb/ stable main' >>   /etc/apt/sources.list"
RUN apt-get update
RUN apt-get install -y google-chrome-unstable --no-install-recommends
#RUN apt-get install -y google-chrome-unstable fonts-ipafont-gothic fonts-wqy-zenhei fonts-thai-tlwg fonts-kacst --no-install-recommends
ENV PUPPETEER_EXECUTABLE_PATH "/usr/bin/google-chrome-unstable"
# -----------------------------------------------------------------------------------------------

ENTRYPOINT ["dotnet", "Mega.WhatsAppAutomator.Api.dll"]
