open --background -a Docker
docker build . -t mega.cloudns.cl/mega-whatsappautomator
docker run \
-p 5001:5000 \
--env DATABASE_NAME=Mega.WhatsAppApi \
--env DATABASE_NEEDS_CERTIFICATE=true \
--env LOCAL_API_PORT=5000 \
--env IS_RUNNING_ON_HEROKU=false \
--env USE_HEADLESS_CHROMIUM=false \
--env CLIENT_ID=dae2844a-25dd-4809-9e56-a49218ac86e6 \
--env INSTANCE_ID=Bot2 \
--env DATABASE_URL=https://a.free.gsoftware.ravendb.cloud/ \
mega.cloudns.cl/mega-whatsappautomator