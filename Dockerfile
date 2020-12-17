FROM gustavogmoraes/mega.puppeteersharp-aspnetcore-3.1-base

EXPOSE 80
EXPOSE 443
EXPOSE 5000

WORKDIR /app
COPY Output/ /app/

ENTRYPOINT ["dotnet", "Mega.WhatsAppAutomator.Api.dll"]