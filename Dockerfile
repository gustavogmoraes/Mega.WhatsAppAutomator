#FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
#
##ARG CHROME_VERSION="84.0.4147.89-1"
#RUN apt-get update
#RUN apt-get update && apt-get -f install && apt-get -y install wget gnupg2 apt-utils
##RUN wget --no-verbose -O /tmp/chrome.deb http://dl.google.com/linux/chrome/deb/pool/main/g/google-chrome-stable/google-chrome-stable_${CHROME_VERSION}_amd64.deb \
#RUN wget --no-verbose -O /tmp/chrome.deb https://dl.google.com/linux/direct/google-chrome-stable_current_amd64.deb \
#&& apt-get install -y /tmp/chrome.deb --no-install-recommends --allow-downgrades fonts-ipafont-gothic fonts-wqy-zenhei fonts-thai-tlwg fonts-kacst fonts-freefont-ttf \
#&& rm /tmp/chrome.deb
#
## Add user, so we don't need --no-sandbox.
#RUN groupadd -r pptruser && useradd -r -g pptruser -G audio,video pptruser \
#    && mkdir -p /home/pptruser/Downloads \
#    && chown -R pptruser:pptruser /home/pptruser
#
#WORKDIR /app
#RUN usermod -G root pptruser
#
##Run everything after as non-privileged user.
#RUN chown -R pptruser:pptruser /app
#USER pptruser
#
#ENV DOTNET_RUNNING_IN_CONTAINER=true
#ENV PUPPETEER_EXECUTABLE_PATH "/usr/bin/google-chrome"

#docker build -t mega.whatsappautomator . && docker run -p 5000:5000 --env IS_DEV_ENV=true --env ASPNETCORE_ENVIRONMENT=Development --env ASPNETCORE_URLS=http://+:5000 -t -i --expose 5000 --security-opt seccomp:unconfined mega.whatsappautomator


FROM mega.puppeteersharp-aspnetcore-3.1-base
ARG environmentType
ENV ASPNETCORE_ENVIRONMENT="Development"
EXPOSE 80
EXPOSE 443

WORKDIR /app
COPY Output/ /app/

ENTRYPOINT ["dotnet", "Mega.WhatsAppAutomator.Api.dll"]