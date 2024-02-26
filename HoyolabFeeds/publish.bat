rem dotnet publish --self-contained -c Release -r linux-x64

docker save hoyolabfeeds -o hoyolabfeeds.tar
docker load < hoyolabfeeds.tar

docker run -d -p 8331:8080 --restart unless-stopped hoyolabfeeds

pause
