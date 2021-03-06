FROM --platform=$BUILDPLATFORM ubuntu:focal AS build

# The latest platform tools version number can
# be found at https://dl.google.com/android/repository/repository2-1.xml
ARG platform_tools_version=31.0.0
ARG tini_version=0.19.0

RUN apt-get update \
&& apt-get install -y curl unzip

RUN curl -L -X GET https://github.com/krallin/tini/releases/download/v${tini_version}/tini-amd64 --output /bin/tini \
&& chmod +x /bin/tini

RUN mkdir -p /build \
&& curl -X GET https://dl.google.com/android/repository/platform-tools_r${platform_tools_version}-linux.zip --output /build/platform-tools_r${platform_tools_version}-linux.zip \
&& unzip -j /build/platform-tools_r${platform_tools_version}-linux.zip platform-tools/adb -d /build \
&& unzip -j /build/platform-tools_r${platform_tools_version}-linux.zip platform-tools/NOTICE.txt -d /build

FROM gcr.io/distroless/cc-debian10

ARG GIT_COMMIT_DATE
ARG GIT_COMMIT_ID
ARG GIT_REF
ARG GIT_REMOTE
ARG VERSION

LABEL org.opencontainers.image.title="adb"
LABEL org.opencontainers.image.description="Android Debug Bridge"
LABEL org.opencontainers.image.licenses="Apache-2.0"

LABEL org.opencontainers.image.created=$GIT_COMMIT_DATE
LABEL org.opencontainers.image.authors="Frederik Carlier <frederik.carlier@quamotion.mobi>"
LABEL org.opencontainers.image.vendor="Quamotion bv <info@quamotion.mobi>"
LABEL org.opencontainers.image.url="https://www.kaponata.io/"
LABEL org.opencontainers.image.source=$GIT_REMOTE
LABEL org.opencontainers.image.version=$VERSION
LABEL org.opencontainers.image.revision=$GIT_COMMIT_ID
LABEL org.opencontainers.image.ref.name=$GIT_REF

COPY --from=build /bin/tini /bin/
COPY --from=build /build/adb /usr/bin/

ENTRYPOINT [ "/bin/tini", "--" ]

CMD [ "/usr/bin/adb", "-a", "-P", "5037", "server", "nodaemon" ]
