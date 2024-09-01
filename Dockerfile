FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /app

RUN apk add --no-cache clang zlib-dev musl-dev

COPY ./src/*.csproj ./
RUN dotnet restore

COPY ./src/ ./
RUN dotnet publish -c Release -o out && rm -rf out/WordCloud.Server.dbg

FROM alpine AS runtime
WORKDIR /app

RUN apk add --no-cache fontconfig

COPY --from=build /app/out .

ENTRYPOINT [ "/app/WordCloud.Server" ]
