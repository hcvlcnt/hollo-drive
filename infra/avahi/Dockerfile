FROM alpine:3.22

RUN apk add --no-cache avahi

COPY avahi-daemon.conf /etc/avahi/avahi-daemon.conf

EXPOSE 5353/udp

CMD ["avahi-daemon", "--no-chroot"]
