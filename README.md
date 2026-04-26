# 🏷️ FoodSocial

<p align="center">
  <a href="https://github.com/NCHzzz/FoodSocial/stargazers">
    <img src="https://img.shields.io/github/stars/NCHzzz/FoodSocial?style=flat&color=FFD93D" alt="Stars"/>
  </a>
  <a href="https://github.com/NCHzzz/FoodSocial/fork">
    <img src="https://img.shields.io/github/forks/NCHzzz/FoodSocial?style=flat&color=6BCB77" alt="Forks"/>
  </a>
  <a href="https://github.com/NCHzzz/FoodSocial/blob/main/LICENSE">
    <img src="https://img.shields.io/github/license/NCHzzz/FoodSocial?style=flat&color=4ECDC4" alt="License"/>
  </a>
  <a href="https://github.com/NCHzzz/FoodSocial/actions/workflows">
    <img src="https://img.shields.io/github/actions/workflow/status/NCHzzz/FoodSocial/ci?branch=main&style=flat" alt="CI Status"/>
  </a>
</p>

---

> 🎯 **AI-native Food-first social platform** | Built with .NET 10 microservices & Angular | 🚀 Launching in Vietnam 🇻🇳

---


---

## 📋 Table of Contents

- [✨ Features](#-features)
- [🛠️ Tech Stack](#🛠️-tech-stack)
- [🏗️ Architecture](#🏗️-architecture)
- [📁 Project Structure](#-project-structure)
- [🚀 Quick Start](#-quick-start)
- [⚙️ Configuration](#️-configuration)
- [🧪 Testing](#-testing)
- [📦 Deployment](#-deployment)
- [📖 Documentation](#-documentation)
- [🤝 Contributing](#-contributing)
- [💬 Community](#-community)
- [📄 License](#-license)
- [🙏 Acknowledgments](#-acknowledgments)

---

## ✨ Features

| Feature | Description |
|---------|-------------|
| 🖼️ **Dish & Place Discovery** | Share and discover dishes and places in your community |
| 👥 **Social Interactions** | Connect with food enthusiasts, follow friends, and join food communities |
| 📸 **Rich Media** | Share photos, videos, and reviews of your culinary experiences |
| 🔍 **Smart Search** | AI-powered recommendations for dishes, restaurants, and food trends |
| 🔔 **Real-time Notifications** | Stay updated with likes, comments, and new followers |
| 🏆 **Leaderboards & Gamification** | Earn badges and climb ranks as a food critic |
| 🍴 **Dish Cataloging** | Create and manage personal dish collections |
| 📍 **Place Management** | Add and review restaurants, street food stalls, and home kitchens |
| ⭐ **Rating & Reviews** | Rate dishes and places with detailed reviews |
| 👀 **Watch List** | Follow dishes, places, and users |

---

## 🛠️ Tech Stack

### Backend 🖥️
<p align="left">
  <img src="https://img.shields.io/badge/.NET_10-512BD4?style=flat&logo=.net&logoColor=white" alt=".NET 10"/>
  <img src="https://img.shields.io/badge/C%23-239120?style=flat&logo=c-sharp&logoColor=white" alt="C#"/>
  <img src="https://img.shields.io/badge/ASP.NET_Core-512BD4?style=flat&logo=aspnetcore&logoColor=white" alt="ASP.NET Core"/>
  <img src="https://img.shields.io/badge/Entity_Framework-512BD4?style=flat&logo=entityframework&logoColor=white" alt="EF Core"/>
  <img src="https://img.shields.io/badge/PostgreSQL-4169E1?style=flat&logo=postgresql&logoColor=white" alt="PostgreSQL"/>
  <img src="https://img.shields.io/badge/RabbitMQ-FF6600?style=flat&logo=rabbitmq&logoColor=white" alt="RabbitMQ"/>
  <img src="https://img.shields.io/badge/Keycloak-FF6B6B?style=flat&logo=keycloak&logoColor=white" alt="Keycloak"/>
</p>

### DevOps 🚀
<p align="left">
  <img src="https://img.shields.io/badge/Docker-2496ED?style=flat&logo=docker&logoColor=white" alt="Docker"/>
  <img src="https://img.shields.io/badge/Kubernetes-326CE8?style=flat&logo=kubernetes&logoColor=white" alt="Kubernetes"/>
  <img src="https://img.shields.io/badge/Helm-2771BB?style=flat&logo=helm&logoColor=white" alt="Helm"/>
  <img src="https://img.shields.io/badge/GitHub_Actions-2088FF?style=flat&logo=github-actions&logoColor=white" alt="GitHub Actions"/>
  <img src="https://img.shields.io/badge/Prometheus-E6522C?style=flat&logo=prometheus&logoColor=white" alt="Prometheus"/>
  <img src="https://img.shields.io/badge/Grafana-F46800?style=flat&logo=grafana&logoColor=white" alt="Grafana"/>
</p>

### Frontend 🎨
<p align="left">
  <img src="https://img.shields.io/badge/Angular-DD0031?style=flat&logo=angular&logoColor=white" alt="Angular"/>
  <img src="https://img.shields.io/badge/TypeScript-3178C6?style=flat&logo=typescript&logoColor=white" alt="TypeScript"/>
  <img src="https://img.shields.io/badge/TailwindCSS-38B2AC?style=flat&logo=tailwind-css&logoColor=white" alt="Tailwind CSS"/>
  <img src="https://img.shields.io/badge/NgRx-BA68C8?style=flat&logo=ngrx&logoColor=white" alt="NgRx"/>
  <img src="https://img.shields.io/badge/SignalR-FF6B6B?style=flat&logo=signalr&logoColor=white" alt="SignalR"/>
</p>

### Patterns & Practices 📐
<p align="left">
  <img src="https://img.shields.io/badge/Microservices-FF6B6B?style=flat" alt="Microservices"/>
  <img src="https://img.shields.io/badge/CQRS-4ECDC4?style=flat" alt="CQRS"/>
  <img src="https://img.shields.io/badge/Event_Sourcing-5865F2?style=flat" alt="Event Sourcing"/>
  <img src="https://img.shields.io/badge/Repository_Pattern-6BCB77?style=flat" alt="Repository"/>
  <img src="https://img.shields.io/badge/Unit_of_Work-4D96FF?style=flat" alt="Unit of Work"/>
  <img src="https://img.shields.io/badge/DDD-FF6B6B?style=flat" alt="DDD"/>
</p>

---

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                              CLIENT (Angular)                            │
│                    http://localhost:4200 | https://foodsocial.vn         │
└─────────────────────────────────────────┬───────────────────────────────┘
                                          │
                                          ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                         API GATEWAY (YARP)                               │
│                    http://localhost:8080 | reverse proxy                │
│                              │                                           │
│         ┌───────────────────┼───────────────────┐                       │
│         ▼                   ▼                   ▼                        │
│   ┌──────────┐       ┌──────────┐       ┌──────────┐                 │
│   │ Identity │       │   Dish   │       │  Place   │                   │
│   │ Service  │       │ Service  │       │ Service  │                   │
│   └────┬─────┘       └────┬─────┘       └────┬─────┘                   │
│        │                  │                  │                          │
│        ▼                  ▼                  ▼                          │
│   ┌─────────────────────────────────────────────────────────┐          │
│   │                    MESSAGE BUS (RabbitMQ)                │          │
│   │         Event-driven communication between services      │          │
│   └─────────────────────────────────────────────────────────┘          │
│        │                  │                  │                          │
│        ▼                  ▼                  ▼                          │
│   ┌──────────┐       ┌──────────┐       ┌──────────┐       ┌──────────┐  │
│   │   Auth   │       │ Database │       │ Database │       │   Notif  │  │
│   │ (Keycloak│◄─────►│ (User)   │       │ (Content)│◄─────►│ Service │  │
│   └──────────┘       └──────────┘       └──────────┘       └──────────┘  │
└─────────────────────────────────────────────────────────────────────────┘
```

### Microservices

| Service | Port | Responsibility |
|---------|------|----------------|
| **API Gateway** | 8080 | Request routing, rate limiting, authentication |
| **Identity Service** | 5001 | User authentication, authorization via Keycloak |
| **Dish Service** | 5002 | Dish CRUD, ratings, reviews |
| **Place Service** | 5003 | Place/Restaurant management |
| **Social Service** | 5004 | Follows, feed, activity |
| **Notification Service** | 5005 | Push notifications, real-time via SignalR |

---

## 📁 Project Structure

```
FoodSocial/
│
├── 📂 Backend/                          # .NET 10 microservices
│   ├── src/
│   │   ├── AppHost/                    # .NET Aspire orchestration
│   │   ├── API Gateway/                 # YARP reverse proxy
│   │   ├── Services/
│   │   │   ├── Identity/               # Auth & user management
│   │   │   ├── Dish/                   # Dish management
│   │   │   ├── Place/                  # Place management
│   │   │   ├── Social/                 # Social features
│   │   │   └── Notification/           # Real-time notifications
│   │   └── Shared/                      # Shared kernel, contracts
│   ├── tests/                           # Integration & unit tests
│   └── docker-compose.yml               # Local development stack
│
├── 📂 Frontend/                         # Angular 17+ application
│   ├── src/
│   │   ├── app/
│   │   │   ├── core/                   # Auth, guards, interceptors
│   │   │   ├── shared/                 # Components, pipes, directives
│   │   │   ├── features/               # Lazy-loaded feature modules
│   │   │   │   ├── home/               # Home & feed
│   │   │   │   ├── dish/               # Dish discovery
│   │   │   │   ├── place/              # Place discovery
│   │   │   │   ├── profile/            # User profiles
│   │   │   │   └── admin/              # Admin panel
│   │   │   └── pages/                  # Standalone pages
│   │   ├── assets/                     # Images, icons, fonts
│   │   └── styles/                      # Global SCSS, themes
│   ├── angular.json
│   └── tailwind.config.js
│
├── 📂 docs/                             # Project documentation
│   ├── project-discovery/              # Product & architecture truth
│   │   ├── product-vision.md            # Product vision & roadmap
│   │   ├── architecture.md              # System architecture
│   │   ├── domain-models.md             # Domain models & entities
│   │   └── api-contracts/                # API contract specifications
│   ├── ai/                              # AI agent rules
│   │   ├── AGENTS.md                    # Agent operating contract
│   │   ├── engineering-rules.md         # Engineering guidelines
│   │   ├── workflows.md                 # Development workflows
│   │   └── stack-rules/                 # Stack-specific rules
│   └── delivery/                        # Release documentation
│
├── 📂 .github/                          # GitHub configuration
│   ├── workflows/                      # CI/CD pipelines
│   ├── ISSUE_TEMPLATE/                # Issue templates
│   └── PULL_REQUEST_TEMPLATE.md        # PR templates
│
└── 📂 openspec/                        # OpenSpec change definitions
```

---

## 🚀 Quick Start

### Prerequisites

| Tool | Version | Installation |
|------|---------|--------------|
| .NET SDK | 10.0+ | [Download](https://dotnet.microsoft.com/download/dotnet/10.0) |
| Node.js | 20+ | [Download](https://nodejs.org/) |
| Docker Desktop | Latest | [Download](https://www.docker.com/products/docker-desktop) |
| Git | 2.0+ | [Download](https://git-scm.com/) |

### Option 1: .NET Aspire (Recommended) 🚀

```bash
# Clone the repository
git clone https://github.com/NCHzzz/FoodSocial.git
cd FoodSocial

# Run with Aspire dashboard
cd Backend/src/AppHost
dotnet run --dashboard

# Access dashboard at http://localhost:18888
```

### Option 2: Docker Compose 🐳

```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down
```

### Option 3: Manual Setup 🔧

**Backend:**
```bash
cd Backend/src/API Gateway
dotnet run

# Or run specific service
cd Backend/src/Services/Dish
dotnet run
```

**Frontend:**
```bash
cd Frontend

# Install dependencies
npm install

# Start development server
npm start

# Build for production
npm run build
```

### Access Points

| Service | URL | Description |
|---------|-----|-------------|
| 🌐 Frontend | http://localhost:4200 | Angular dev server |
| 🔌 API Gateway | http://localhost:8080 | Main API endpoint |
| 📊 Aspire Dashboard | http://localhost:18888 | Service orchestration |
| 🐘 PostgreSQL | localhost:5432 | Database |
| 🐰 RabbitMQ | http://localhost:15672 | Message queue admin |
| 🔐 Keycloak | http://localhost:8080/auth | Identity provider |

---

## ⚙️ Configuration

### Environment Variables

Create a `.env` file in `Backend/src/`:

```env
# Database
ConnectionStrings__DefaultConnection=Host=localhost;Database=foodsocial;Username=postgres;Password=your_password

# RabbitMQ
RabbitMQ__Host=localhost
RabbitMQ__Port=5672
RabbitMQ__Username=guest
RabbitMQ__Password=guest

# Keycloak
Keycloak__Url=http://localhost:8080/auth
Keycloak__Realm=foodsocial
Keycloak__ClientId=foodsocial-api

# JWT
Jwt__Key=your-256-bit-secret-key-here
Jwt__Issuer=FoodSocial
Jwt__Audience=FoodSocialAPI
Jwt__ExpirationMinutes=60

# Frontend URL
AppSettings__FrontendUrl=http://localhost:4200

# Logging
Logging__LogLevel__Default=Information
```

### Frontend Configuration

Update `Frontend/src/environments/`:

```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:8080',
  keycloakUrl: 'http://localhost:8080/auth',
  keycloakRealm: 'foodsocial',
  keycloakClientId: 'foodsocial-web'
};
```

---

## 🧪 Testing

### Backend Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test --filter "FullyQualifiedName~DishServiceTests"
```

### Frontend Tests

```bash
# Run unit tests
ng test

# Run with coverage
ng test --code-coverage

# Run e2e tests
ng e2e
```

### Test Coverage

| Type | Tool | Coverage Target |
|------|------|-----------------|
| Unit Tests | xUnit, NUnit | 80%+ |
| Integration Tests | TestContainers | Core flows |
| E2E Tests | Playwright | Critical paths |

---

## 📦 Deployment

### Docker 🐳

```bash
# Build all services
docker-compose build

# Run production stack
docker-compose -f docker-compose.prod.yml up -d

# Scale specific service
docker-compose up -d --scale dish-service=3
```

### Kubernetes ☸️

```bash
# Add Helm repo
helm repo add foodsocial https://charts.foodsocial.vn

# Install chart
helm install foodsocial foodsocial/foodsocial \
  --set image.tag=v1.0.0 \
  --set ingress.enabled=true \
  --set ingress.host=foodsocial.vn

# Upgrade
helm upgrade foodsocial foodsocial/foodsocial \
  --set image.tag=v1.1.0
```

### Cloud Providers

| Provider | Guide |
|----------|-------|
| 🟣 Azure | [Deploy to Azure Container Apps](./docs/deployment/azure.md) |
| 🟢 AWS | [Deploy to EKS](./docs/deployment/aws.md) |
| 🔵 GCP | [Deploy to GKE](./docs/deployment/gcp.md) |

---

## 📖 Documentation

📚 **Explore our documentation:**

- 📘 [Project Discovery](./docs/project-discovery/) - Product vision & specifications
- 🏗️ [Architecture](./docs/project-discovery/architecture.md) - System design
- 📐 [Domain Models](./docs/project-discovery/domain-models.md) - Entity definitions
- 🤖 [AI Agent Rules](./docs/ai/AGENTS.md) - Engineering guidelines
- 🛠️ [API Reference](./docs/api/) - REST API documentation
- 🧩 [Stack Rules](./docs/ai/stack-rules/) - Framework-specific guidelines
- 📦 [Release Notes](./CHANGELOG.md) - Version history

---

## 🤝 Contributing

We welcome contributions! Please follow these steps:

### 1. Setup Development Environment

```bash
# Fork the repository
# Clone your fork
git clone https://github.com/YOUR_USERNAME/FoodSocial.git
cd FoodSocial

# Add upstream remote
git remote add upstream https://github.com/NCHzzz/FoodSocial.git
```

### 2. Create a Feature Branch

```bash
# Update main branch
git fetch upstream
git checkout main
git merge upstream/main

# Create feature branch
git checkout -b feature/your-feature-name
```

### 3. Make Changes & Test

```bash
# Make your changes
# Write tests
# Run tests locally
dotnet test  # Backend
npm test     # Frontend
```

### 4. Commit & Push

```bash
# Stage changes
git add .

# Commit with descriptive message
git commit -m "feat: add new feature description"

# Push to your fork
git push origin feature/your-feature-name
```

### 5. Open a Pull Request

> ⚠️ **Important:** All PRs must pass CI checks and be reviewed before merging.

- Fill out the PR template completely
- Reference related issues
- Include screenshots for UI changes
- Update documentation if needed

### Coding Standards

| Area | Standard |
|------|-----------|
| 📝 Commits | [Conventional Commits](https://www.conventionalcommits.org/) |
| 🎨 Code Style | [StyleCop](https://github.com/DotNetAnalyzers/StyleCopAnalyzers) (Backend), [ESLint](https://eslint.org/) (Frontend) |
| ✅ Tests | Minimum 80% coverage for new code |
| 📖 Docs | Update README and inline docs for public APIs |

---

## 💬 Community

<p align="center">
  <a href="https://discord.gg/foodsocial">
    <img src="https://img.shields.io/discord/123456789?label=Join%20our%20Discord&style=for-the-badge&color=5865F2" alt="Discord"/>
  </a>
  <a href="https://twitter.com/foodsocial">
    <img src="https://img.shields.io/twitter/follow/foodsocial?style=for-the-badge&color=1DA1F2" alt="Twitter"/>
  </a>
</p>

- 💬 **Discord** - Join our community chat
- 🐦 **Twitter** - Follow for updates
- 📧 **Email** - Contact: hello@foodsocial.vn
- 📝 **Blog** - Read our [engineering blog](https://blog.foodsocial.vn)

---

## 📄 License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.

```
MIT License

Copyright (c) 2024-present NCHzzz and FoodSocial Contributors

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

---

## 🙏 Acknowledgments

<div align="center">

| Thanks to | For |
|-----------|-----|
| [Awesome Readme](https://github.com/matiassingers/awesome-readme) | README inspiration |
| [OpenCode](https://opencode.ai/) | AI coding assistant |
| [Microsoft](https://dotnet.microsoft.com/) | .NET ecosystem |
| [Angular](https://angular.io/) | Frontend framework |
| [Keycloak](https://www.keycloak.org/) | Identity & access management |
| [All Contributors](https://github.com/NCHzzz/FoodSocial/graphs/contributors) | Making this project awesome |

</div>

---

<p align="center">
  <a href="#top">
    <img src="https://img.shields.io/badge/Back%20to%20top-↑-FF6B6B?style=flat" alt="Back to top"/>
  </a>
</p>

<p align="center">
  Made with ❤️ by <a href="https://github.com/NCHzzz">NCHzzz</a> • 🇻🇳 Vietnam
</p>