# Loopai Documentation

**Version**: v0.3
**Last Updated**: 2025-10-27

Welcome to the Loopai documentation! This directory contains comprehensive guides for using, developing, and deploying the Loopai framework.

---

## 📚 Documentation Index

### Getting Started

**New to Loopai?** Start here:

| Document | Description | Status |
|----------|-------------|--------|
| [Getting Started](GETTING_STARTED.md) | Installation and first steps | ✅ Current |
| [Development Guide](DEVELOPMENT.md) | Local development setup | ✅ Current |
| [Deployment Guide](DEPLOYMENT.md) | Docker and Kubernetes deployment | ✅ Current |

### Architecture & Design

Understanding the system:

| Document | Description | Status |
|----------|-------------|--------|
| [Architecture Overview](ARCHITECTURE.md) | System design and components | ✅ Current |
| [Hybrid Architecture](ARCHITECTURE_HYBRID.md) | Python + C# integration | ✅ Current |

### SDK Documentation

Client SDK guides:

| SDK | Documentation | Status |
|-----|---------------|--------|
| .NET | [.NET SDK](../sdk/dotnet/README.md) | ✅ v0.1.0 |
| Python | [Python SDK](../sdk/python/README.md) | ✅ v0.1.0 |
| TypeScript | [TypeScript SDK](../sdk/typescript/README.md) | ✅ v0.1.0 |
| Integration Tests | [Test Guide](../tests/integration/README.md) | ✅ Complete |

### Development Guides

Advanced development topics:

| Document | Description | Status |
|----------|-------------|--------|
| [Plugin Development](PLUGIN_DEVELOPMENT_GUIDE.md) | Creating custom plugins | ✅ Current |
| [Phase 6 Plan](PHASE6_PLAN.md) | SDK and extensibility planning | ✅ Complete |

### Phase Documentation

#### Current Phase (Complete)
| Document | Description | Status |
|----------|-------------|--------|
| [Phase 10-11: SDK Complete](PHASE10-11_SDK_COMPLETE.md) | Multi-language SDKs and integration testing | ✅ v0.3 |

#### Recent Phases
| Document | Description | Status |
|----------|-------------|--------|
| [Phase 9: Python SDK](PHASE9_PYTHON_SDK_COMPLETE.md) | Python Client SDK | ✅ Complete |
| [Phase 8: .NET SDK](PHASE8_COMPLETE.md) | .NET Client SDK | ✅ Complete |
| [Phase 8.1: Integration Tests](PHASE8.1_INTEGRATION_TESTS_COMPLETE.md) | .NET integration testing | ✅ Complete |
| [Phase 8 Status](PHASE8_STATUS.md) | .NET SDK details | ✅ Complete |
| [Phase 7.2 Status](PHASE7.2_STATUS.md) | Batch API optimization | ✅ Complete |
| [Phase 7.1 Status](PHASE7.1_STATUS.md) | CodeBeaker integration | ✅ Complete |
| [Phase 6.2 Status](PHASE6.2_STATUS.md) | Plugin system | ✅ Complete |
| [Phase 6.1 Status](PHASE6.1_STATUS.md) | .NET Client SDK | ✅ Complete |
| [Phase 5 Status](PHASE5_STATUS.md) | Framework infrastructure | ✅ Complete |

### Historical Documentation

See [archive/](archive/) for earlier phase documentation:
- Phase 0-4 planning and status documents
- Research notes and legacy task lists

---

## 📖 Quick Navigation

### By Role

**For Developers**:
1. Start with [Getting Started](GETTING_STARTED.md)
2. Read [Development Guide](DEVELOPMENT.md)
3. Explore [Architecture Overview](ARCHITECTURE.md)
4. Check [Phase 6 Plan](PHASE6_PLAN.md) for SDK development

**For DevOps**:
1. Read [Deployment Guide](DEPLOYMENT.md)
2. Review [Phase 5 Status](PHASE5_STATUS.md) for infrastructure features
3. Check Helm chart docs in `/helm/loopai/README.md`

**For Architects**:
1. Read [Architecture Overview](ARCHITECTURE.md)
2. Study [Hybrid Architecture](ARCHITECTURE_HYBRID.md)
3. Review [Phase 5 Status](PHASE5_STATUS.md) for framework design decisions

### By Topic

**Installation & Setup**:
- [Getting Started](GETTING_STARTED.md) - Quick installation
- [Development Guide](DEVELOPMENT.md) - Local setup
- [Deployment Guide](DEPLOYMENT.md) - Production deployment

**Architecture**:
- [Architecture Overview](ARCHITECTURE.md) - System design
- [Hybrid Architecture](ARCHITECTURE_HYBRID.md) - Framework architecture
- [Phase 5 Status](PHASE5_STATUS.md) - Infrastructure patterns

**Features**:
- [Phase 5 Status](PHASE5_STATUS.md) - Metrics, webhooks, health checks
- [Phase 6 Plan](PHASE6_PLAN.md) - SDKs, plugins, batch operations

**Development**:
- [Development Guide](DEVELOPMENT.md) - Local development
- [Phase 6 Plan](PHASE6_PLAN.md) - SDK and plugin development

---

## 🎯 Current Version: v0.3

**Status**: Production-ready SDK ecosystem with comprehensive testing

**Highlights**:
- ✅ 170+ tests passing (42 integration, 128+ unit)
- ✅ Multi-language SDKs (.NET, Python, TypeScript)
- ✅ 100% cross-SDK compatibility verified
- ✅ Plugin system for extensibility
- ✅ Batch operations API
- ✅ CodeBeaker multi-language execution
- ✅ Kubernetes-ready with Helm charts
- ✅ CI/CD integration ready

**Next**: v0.4 - Enterprise Features (Multi-tenancy, SSO, Advanced Analytics)

---

## 📝 Documentation Standards

All Loopai documentation follows these standards:

### File Naming
- `UPPERCASE.md` for major documents (ARCHITECTURE.md, DEPLOYMENT.md)
- `PHASE*_*.md` for phase-specific documents
- `lowercase.md` for supporting docs (readme.md)

### Structure
- Clear headings with emoji indicators
- Code examples with syntax highlighting
- Tables for comparisons and references
- Diagrams for architecture (ASCII or links)

### Status Indicators
- ✅ Complete/Current
- 📋 In Progress/Planning
- ⚠️ Partial/Issues
- ❌ Failed/Deprecated
- 📝 Draft

### Updates
- Version numbers in headers
- Last updated date
- Status badges
- Migration guides for breaking changes

---

## 🔄 Documentation Lifecycle

### Active Documents
Documents for current and next version:
- **Current**: Phase 5 Status (v0.2)
- **Next**: Phase 6 Plan (v0.3)
- **Maintained**: Getting Started, Architecture, Development, Deployment

### Archived Documents
Completed phase documents moved to `archive/`:
- Phase 0-4 status and planning documents
- Historical research and task documents

### Deprecation Process
1. Mark document as deprecated with warning
2. Link to replacement document
3. Move to archive after one version
4. Keep in archive for reference

---

## 🤝 Contributing to Documentation

### Documentation Needs
- **Tutorials**: Step-by-step guides for common tasks
- **Examples**: Real-world usage scenarios
- **API Reference**: Detailed endpoint documentation
- **Troubleshooting**: Common issues and solutions

### How to Contribute
1. Check [CONTRIBUTING.md](../CONTRIBUTING.md) for guidelines
2. Follow documentation standards above
3. Submit PRs with clear descriptions
4. Include examples and diagrams where helpful

### Review Process
- Technical accuracy review
- Clarity and readability review
- Code example testing
- Link validation

---

## 📞 Support

- **Issues**: [GitHub Issues](https://github.com/iyulab/loopai/issues)
- **Discussions**: [GitHub Discussions](https://github.com/iyulab/loopai/discussions)
- **Documentation Issues**: Tag with `documentation` label

---

## 📚 External Resources

### Framework Documentation
- [ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
- [Prometheus](https://prometheus.io/docs/)
- [Kubernetes](https://kubernetes.io/docs/)
- [Helm](https://helm.sh/docs/)

### Related Technologies
- [Deno](https://deno.land/manual) - Edge runtime
- [OpenTelemetry](https://opentelemetry.io/docs/) - Distributed tracing
- [Redis](https://redis.io/documentation) - Caching

---

**Last Updated**: October 27, 2025
**Version**: v0.3 (Phase 10-11 Complete)
**Next Release**: v0.4 (Enterprise Features)
