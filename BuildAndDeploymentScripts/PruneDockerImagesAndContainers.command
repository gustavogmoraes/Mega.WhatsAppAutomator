open --background -a Docker
docker rmi -f $(docker images -a -q)
echo 'Done'