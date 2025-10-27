# Loopai Documentation

Complete documentation for the Loopai program synthesis and execution framework.

---

## 📚 Documentation Index

### Getting Started

| Document | Description | Status |
|----------|-------------|--------|
| [Getting Started](GETTING_STARTED.md) | Installation, first steps, and basic usage | ✅ Current |
| [Development Guide](DEVELOPMENT.md) | Local development environment setup | ✅ Current |
| [Deployment Guide](DEPLOYMENT.md) | Docker and Kubernetes deployment | ✅ Current |

### Architecture & Design

| Document | Description | Status |
|----------|-------------|--------|
| [Architecture Overview](ARCHITECTURE.md) | System design, components, and data flow | ✅ Current |
| [Hybrid Architecture](ARCHITECTURE_HYBRID.md) | Python + C# framework design | ✅ Current |

### Project Status

| Document | Description | Status |
|----------|-------------|--------|
| [Phase 5 Status](PHASE5_STATUS.md) | Framework infrastructure features (Complete) | ✅ v0.2 |
| [Phase 6 Plan](PHASE6_PLAN.md) | SDK development and extensibility | 📋 In Progress |

### Historical Documentation

Archived documentation for completed phases:

| Document | Description | Phase |
|----------|-------------|-------|
| [Phase 0 Status](archive/PHASE0_STATUS.md) | Basic classification and validation | Phase 0 ✅ |
| [Phase 1 Status](archive/PHASE1_STATUS.md) | Multi-class support | Phase 1 ✅ |
| [Phase 2 Status](archive/PHASE2_STATUS.md) | Pattern recognition | Phase 2 ⚠️ |
| [Phase 3 Plan](archive/PHASE3_PLAN.md) | Edge runtime planning | Phase 3 📋 |
| [Phase 3 Status](archive/PHASE3_STATUS.md) | Edge runtime implementation | Phase 3 ✅ |
| [Phase 4 Plan](archive/PHASE4_PLAN.md) | Hybrid architecture planning | Phase 4 📋 |
| [Phase 4 Status](archive/PHASE4_STATUS.md) | C# Cloud API implementation | Phase 4 ✅ |
| [Phase 4.1 Work Items](archive/PHASE4.1_WORKITEMS.md) | Detailed API work items | Phase 4.1 ✅ |
| [Phase 4 Transition](archive/PHASE4_TRANSITION_SUMMARY.md) | Transition summary | Phase 4 📝 |

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

## 🎯 Current Version: v0.2

**Status**: Production-ready framework with complete infrastructure

**Highlights**:
- ✅ 117/117 tests passing
- ✅ Kubernetes-ready with Helm charts
- ✅ Prometheus metrics and OpenTelemetry
- ✅ Webhook event system
- ✅ Production health checks

**Next**: v0.3 - SDK & Extensibility (Phase 6)

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

**Last Updated**: October 26, 2025
**Version**: v0.2 (Phase 5 Complete)
**Next Release**: v0.3 (Phase 6 - SDK Development)
