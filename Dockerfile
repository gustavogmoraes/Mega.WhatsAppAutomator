FROM gustavogmoraes/mega.puppeteersharp-aspnetcore-3.1-base

ENV DATABASE_NAME="Mega.WhatsAppApi" \
    DATABASE_URL="https://a.free.gsoftware.ravendb.cloud/" \
    DATABASE_NEEDS_CERT="true" \
    LOCAL_API_PORT="5000" \
    IS_RUNNING_ON_HEROKU="false" \
    USE_HEADLESS_CHROMIUM="true" \
    INSTANCE_ID="Bot2NoxMegaServer"
    
EXPOSE 80
EXPOSE 443
EXPOSE 5000
EXPOSE 5001

WORKDIR /app
COPY Output/* /app/

ENTRYPOINT ["dotnet", "Mega.WhatsAppAutomator.Api.dll"]