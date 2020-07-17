FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS base
WORKDIR /app
EXPOSE 80
#EXPOSE 443
ENV ASPNETCORE_URLS=http://+:80;#https://+:443;
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

# --------------------------- Setting up Puppeteer dependencies ---------------------------------
RUN apt-get update
# for https
RUN apt-get install -yyq ca-certificates
# install libraries
RUN apt-get install -yyq libappindicator1 libasound2 libatk1.0-0 libc6 libcairo2 libcups2 libdbus-1-3 libexpat1 libfontconfig1 libgcc1 libgconf-2-4 libgdk-pixbuf2.0-0 libglib2.0-0 libgtk-3-0 libnspr4 libnss3 libpango-1.0-0 libpangocairo-1.0-0 libstdc++6 libx11-6 libx11-xcb1 libxcb1 libxcomposite1 libxcursor1 libxdamage1 libxext6 libxfixes3 libxi6 libxrandr2 libxrender1 libxss1 libxtst6
# tools
RUN apt-get install -yyq gconf-service lsb-release wget xdg-utils
# and fonts
RUN apt-get install -yyq fonts-liberation

ARG CHROME_VERSION="84.0.4147.89-1"
RUN apt-get update && apt-get -f install && apt-get -y install wget gnupg2 apt-utils
RUN wget --no-verbose -O /tmp/chrome.deb http://dl.google.com/linux/chrome/deb/pool/main/g/google-chrome-stable/google-chrome-stable_${CHROME_VERSION}_amd64.deb \
&& apt-get update \
&& apt-get install -y /tmp/chrome.deb --no-install-recommends --allow-downgrades fonts-ipafont-gothic fonts-wqy-zenhei fonts-thai-tlwg fonts-kacst fonts-freefont-ttf \
&& rm /tmp/chrome.deb

#RUN apt-get update && apt-get install -y apt-transport-https
#RUN apt-get update && apt-get -f install && apt-get -y install wget gnupg2 apt-utils -y
#RUN wget -q -O - https://dl-ssl.google.com/linux/linux_signing_key.pub | apt-key add -
#RUN sh -c "echo 'deb http://dl.google.com/linux/chrome/deb/ stable main' >>   /etc/apt/sources.list"
#RUN apt-get update
##RUN apt-get install -y google-chrome-unstable --no-install-recommends
#RUN apt-get install -y google-chrome-unstable fonts-ipafont-gothic fonts-wqy-zenhei fonts-thai-tlwg fonts-kacst --no-install-recommends

## Add user, so we don't need --no-sandbox.
## same layer as npm install to keep re-chowned files from using up several hundred MBs more space    
#RUN groupadd -r pptruser && useradd -r -g pptruser -G audio,video pptruser \
    #&& mkdir -p /home/pptruser/Downloads \
    #&& chown -R pptruser:pptruser /home/pptruser
#
## Run everything after as non-privileged user.
#USER pptruser

ENV PUPPETEER_EXECUTABLE_PATH "/usr/bin/google-chrome-stable"
# -----------------------------------------------------------------------------------------------

FROM publish AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Mega.WhatsAppAutomator.Api.dll"]
