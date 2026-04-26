# 🍔 FoodSocial

<p align="center">
  <img src="https://raw.githubusercontent.com/NCHzzz/FoodSocial/main/.github/assets/banner.svg" alt="FoodSocial Banner" width="100%"/>
</p>

<p align="center">
  <a href="https://github.com/NCHzzz/FoodSocial/releases">
    <img src="https://img.shields.io/github/v/release/NCHzzz/FoodSocial?include_prereleases&style=flat&color=FF6B6B" alt="Latest Release"/>
  </a>
  <a href="https://github.com/NCHzzz/FoodSocial/actions/workflows">
    <img src="https://img.shields.io/github/actions/workflow/status/NCHzzz/FoodSocial/ci?branch=main&style=flat" alt="CI Status"/>
  </a>
  <a href="https://github.com/NCHzzz/FoodSocial/blob/main/LICENSE">
    <img src="https://img.shields.io/github/license/NCHzzz/FoodSocial?style=flat&color=4ECDC4" alt="License"/>
  </a>
  <a href="https://discord.gg/foodsocial">
    <img src="https://img.shields.io/discord/123456789?label=Discord&style=flat&color=5865F2" alt="Discord"/>
  </a>
</p>

> AI-native Food-first social platform built with .NET 10 microservices and Angular

## ✨ Features

- 🖼️ **Dish & Place Discovery** - Share and discover dishes and places in your community
- 👥 **Social Interactions** - Connect with food enthusiasts, follow friends, and join food communities
- 📸 **Rich Media** - Share photos, videos, and reviews of your culinary experiences
- 🔍 **Smart Search** - AI-powered recommendations for dishes, restaurants, and food trends
- 🔔 **Real-time Notifications** - Stay updated with likes, comments, and new followers
- 🏆 **Leaderboards & Gamification** - Earn badges and climb ranks as a food critic

## 🛠️ Tech Stack

### Backend
<p align="left">
  <img src="https://img.shields.io/badge/.NET-10-512BD4?style=flat&logo=.net&logoColor=white" alt=".NET 10"/>
  <img src="https://img.shields.io/badge/C%23-239120?style=flat&logo=c-sharp&logoColor=white" alt="C#"/>
  <img src="https://img.shields.io/badge/PostgreSQL-4169E1?style=flat&logo=postgresql&logoColor=white" alt="PostgreSQL"/>
  <img src="https://img.shields.io/badge/RabbitMQ-FF6600?style=flat&logo=rabbitmq&logoColor=white" alt="RabbitMQ"/>
  <img src="https://img.shields.io/badge/Keycloak-FF6B6B?style=flat&logo=keycloak&logoColor=white" alt="Keycloak"/>
  <img src="https://img.shields.io/badge/Docker-2496ED?style=flat&logo=docker&logoColor=white" alt="Docker"/>
  <img src="https://img.shields.io/badge/Kubernetes-326CE8?style=flat&logo=kubernetes&logoColor=white" alt="Kubernetes"/>
</p>

### Frontend
<p align="left">
  <img src="https://img.shields.io/badge/Angular-DD0031?style=flat&logo=angular&logoColor=white" alt="Angular"/>
  <img src="https://img.shields.io/badge/TypeScript-3178C6?style=flat&logo=typescript&logoColor=white" alt="TypeScript"/>
  <img src="https://img.shields.io/badge/TailwindCSS-38B2AC?style=flat&logo=tailwind-css&logoColor=white" alt="Tailwind CSS"/>
  <img src="https://img.shields.io/badge/NgRx-BA68C8?style=flat&logo=ngrx&logoColor=white" alt="NgRx"/>
</p>

### Architecture
<p align="left">
  <img src="https://img.shields.io/badge/Microservices-FF6B6B?style=flat" alt="Microservices"/>
  <img src="https://img.shields.io/badge/CQRS-4ECDC4?style=flat" alt="CQRS"/>
  <img src="https://img.shields.io/badge/Event%20Sourcing-5865F2?style=flat" alt="Event Sourcing"/>
  <img src="https://img.shields.io/badge/API%20Gateway-FF6B6B?style=flat" alt="API Gateway"/>
  <img src="https://img.shields.io/badge/Message%20Queue-FF6600?style=flat" alt="Message Queue"/>
</p>

## 📁 Project Structure

```
FoodSocial/
├── Backend/                    # .NET 10 microservices
│   ├── src/
│   │   ├── API Gateway/         # API Gateway service
│   │   ├── Identity Service/    # Authentication & Authorization
│   │   ├── Dish Service/       # Dish management
│   │   ├── Place Service/      # Place management
│   │   ├── Social Service/     # Social interactions
│   │   └── Notification/       # Real-time notifications
│   └── tests/                  # Backend tests
│
├── Frontend/                   # Angular application
│   ├── src/
│   │   ├── app/
│   │   │   ├── core/           # Core modules (auth, guards, interceptors)
│   │   │   ├── shared/         # Shared components, pipes, directives
│   │   │   ├── features/       # Feature modules
│   │   │   └── pages/          # Page components
│   │   ├── assets/             # Static assets
│   │   └── styles/             # Global styles, themes
│   └── e2e/                    # End-to-end tests
│
├── docs/                       # Project documentation
│   ├── project-discovery/      # Product & architecture truth
│   ├── ai/                     # AI agent rules & workflows
│   └── delivery/               # Delivery documentation
│
└── openspec/                   # OpenSpec change definitions
```

## 🚀 Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 20+](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [PostgreSQL 16+](https://www.postgresql.org/)
- [RabbitMQ](https://www.rabbitmq.com/)

### Local Development

1. **Clone the repository**
```bash
git clone https://github.com/NCHzzz/FoodSocial.git
cd FoodSocial
```

2. **Start with .NET Aspire** (Recommended)
```bash
# Start all services
dotnet run --project Backend/src/AppHost/AppHost.csproj

# Or use the dashboard
dotnet run --project Backend/src/AppHost/AppHost.csproj --dashboard
```

3. **Or start manually**

   **Backend:**
```bash
cd Backend/src/API Gateway
dotnet run
```

   **Frontend:**
```bash
cd Frontend
npm install
npm start
```

4. **Access the application**
- Frontend: http://localhost:4200
- API: http://localhost:5000
- API Gateway: http://localhost:8080

## 📖 Documentation

- 📘 [Project Discovery](./docs/project-discovery/) - Product & architecture specifications
- 🤖 [AI Agent Rules](./docs/ai/) - Engineering guidelines for AI agents
- 🏗️ [Architecture Overview](./docs/project-discovery/architecture.md) - System design documentation
- 📝 [API Documentation](./docs/api/) - REST API reference

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guide](./CONTRIBUTING.md) for details.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- [Awesome Readme](https://github.com/matiassingers/awesome-readme) - README inspiration
- [OpenCode](https://opencode.ai/) - AI coding assistant
- All contributors and maintainers

---

<p align="center">
  Made with ❤️ by <a href="https://github.com/NCHzzz">NCHzzz</a>
</p>