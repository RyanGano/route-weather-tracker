# Stage 1: build
FROM node:22-alpine AS build
WORKDIR /app

# Install dependencies
COPY package.json package-lock.json* ./
RUN npm ci

# Copy source and build
COPY . .
RUN npm run build

# Stage 2: serve with vite preview
# Uses the same Node image — vite preview is already in devDependencies
FROM node:22-alpine AS serve
WORKDIR /app

COPY --from=build /app/package.json ./
COPY --from=build /app/package-lock.json* ./
RUN npm ci --include=dev

COPY --from=build /app/dist ./dist
COPY --from=build /app/vite.config.ts ./

EXPOSE 4173

CMD ["npm", "start"]
