# Stage 1: build
FROM node:22-alpine AS build
WORKDIR /app

# Install dependencies
COPY package.json yarn.lock* ./
RUN yarn install --frozen-lockfile

# Copy source and build
COPY . .
RUN yarn build

# Stage 2: serve with vite preview
# Uses the same Node image — vite preview is already in devDependencies
FROM node:22-alpine AS serve
WORKDIR /app

COPY --from=build /app/package.json ./
COPY --from=build /app/yarn.lock* ./
RUN yarn install --frozen-lockfile --production=false

COPY --from=build /app/dist ./dist
COPY --from=build /app/vite.config.ts ./

EXPOSE 4173

CMD ["yarn", "start"]
