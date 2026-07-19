FROM node:22-bookworm-slim AS build
WORKDIR /app

COPY package*.json ./
RUN npm ci
RUN ROLLUP_VERSION=$(node -p "require('./package-lock.json').packages['node_modules/rollup'].version") \
    && case "$(uname -m)" in \
        aarch64|arm64) ROLLUP_ARCH=arm64 ;; \
        x86_64|amd64) ROLLUP_ARCH=x64 ;; \
        *) echo "Unsupported Docker build architecture: $(uname -m)" && exit 1 ;; \
    esac \
    && npm install --no-save "@rollup/rollup-linux-${ROLLUP_ARCH}-gnu@${ROLLUP_VERSION}"

COPY . ./
ARG VITE_API_URL=http://localhost:5001/api/
ENV VITE_API_URL=$VITE_API_URL
RUN npm run build

FROM nginx:1.27-alpine AS final
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html/app

EXPOSE 80
