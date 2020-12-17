open --background -a Docker
docker build . -t mega-whatsappautomator
echo 'Saving tar image'
docker save -o mega-whatsappautomator.tar mega-whatsappautomator
echo 'Done'