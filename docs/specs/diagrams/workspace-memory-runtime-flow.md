**Document title:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> Workspace Memory Runtime Flow ✨  
**Prepared by:** Umberto Giacobbi  
**Organization:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> 🚀  
- **Intended use:** A compact diagram showing how bundled assets, workspace files, candidate staging, and promotion currently connect in YAi.  

## Author Profile

Umberto Giacobbi is a founder, consultant, advisor, developer, and operator with international experience across Italy, Switzerland, the United States, Indonesia, and Vietnam. His work spans projects in Europe, the US, and Southeast Asia, with a focus on practical execution, strategic thinking, and technology-led business building.

## Contact Information

- **Email:** [hello@umbertogiacobbi.biz](mailto:hello@umbertogiacobbi.biz)  
- **LinkedIn:** [linkedin.com/in/umbertogiacobbi](https://www.linkedin.com/in/umbertogiacobbi/)  
- **Website:** [umbertogiacobbi.biz](https://umbertogiacobbi.biz)  

## AI Use and Responsibility Notice

This document may include content generated, refined, or reviewed with the assistance of one or more AI models. It should be reviewed and validated before external distribution or operational use. Final responsibility for its verification, interpretation, and application remains with the author(s) and the organization.

# Workspace Memory Runtime Flow

```mermaid
flowchart TD
    A[Bundled templates and skills] --> B[WorkspaceProfileService]
    B --> C[User workspace]
    C --> D[PromptAssetService]
    C --> E[RegexRegistry]
    C --> F[SkillLoader]

    G[Dream pass or future extraction pass] --> H[CandidateStore]
    H --> I[candidates.jsonl]
    H --> J[DREAMS.md projection]

    J --> K[PromotionService]
    K --> L[MemoryTransactionManager]
    L --> M[workspace/.backups/YYYYMMDD]
    L --> N[workspace/memory/*.md]
```

Current gap: the services for staged extraction exist, but only the dream pass is wired into the active CLI dispatcher today.