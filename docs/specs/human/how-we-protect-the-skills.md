**Document title:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> How YAi! Protects Its Built-In Skills ✨  
**Prepared by:** Umberto Giacobbi  
**Organization:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> 🚀  
- **Intended use:** A plain-English explanation of how YAi! checks that its official built-in skills, templates, and prompt files have not been changed in unsafe or unexpected ways.  

## Author Profile

Umberto Giacobbi is a founder, consultant, advisor, developer, and operator with international experience across Italy, Switzerland, the United States, Indonesia, and Vietnam. His work spans projects in Europe, the US, and Southeast Asia, with a focus on practical execution, strategic thinking, and technology-led business building.

## Contact Information

- **Email:** [hello@umbertogiacobbi.biz](mailto:hello@umbertogiacobbi.biz)  
- **LinkedIn:** [linkedin.com/in/umbertogiacobbi](https://www.linkedin.com/in/umbertogiacobbi/)  
- **Website:** [umbertogiacobbi.biz](https://umbertogiacobbi.biz)  

## AI Use and Responsibility Notice

This document may include content generated, refined, or reviewed with the assistance of one or more AI models. It should be reviewed and validated before external distribution or operational use. Final responsibility for its verification, interpretation, and application remains with the author(s) and the organization.

# How YAi! Protects Its Built-In Skills

## 1. The Simple Version

YAi! treats its official built-in skills a bit like sealed ingredients in a trusted kitchen. Before it uses them, it checks that the label still matches the contents and that nobody quietly swapped anything out.

That matters because built-in skills, templates, and prompt assets are part of what YAi! trusts by default. If those files could change silently, then the system could start behaving differently without anyone noticing. YAi! is designed to avoid that kind of quiet surprise.

So the rule is simple: before YAi! loads its own official resources as trusted, it verifies that they are still exactly the files the project intended to ship.

## 2. What Is Being Protected

The protected files live under `src/YAi.Resources/reference/`. That folder contains official YAi! resources such as built-in skill definitions, workspace templates, and prompt-related assets.

These are not the same as ordinary user files. They are part of YAi!'s trusted starting kit. In plain English, they are the files YAi! is allowed to treat as "official" rather than "just something it found on disk."

User-created content is different. If you write your own notes, templates, or third-party skills, YAi! does not automatically pretend those are official YAi! resources. That boundary is intentional.

## 3. How the Protection Works

The protection model is based on a signed manifest. If that phrase sounds technical, think of it as a tamper-evident packing list.

YAi! keeps three important files with the official resources:

- A public key that helps confirm the signature is genuine.
- A manifest that lists the official files and their details.
- A digital signature that proves the manifest came from the trusted signer.

When YAi! starts checking its resources, it does not just trust the folder because the folder exists. It first checks that the manifest itself has a valid signature. Then it checks each listed file to make sure the file still exists and still matches the recorded details.

If everything matches, YAi! treats those resources as trusted.

If something does not match, YAi! does not quietly shrug and continue as if nothing happened. It stops trusting those built-in resources and reports the problem.

## 4. Why This Matters

This is not security theater. It supports one of YAi!'s core ideas: trust should be earned, checked, and visible.

Without this kind of verification, a built-in skill could be edited, replaced, or corrupted and YAi! might load it anyway. That would be the software version of finding that someone changed the recipe card after the meal was already served.

With signing and verification in place, YAi! can say something much more useful:

"These are the official files we expected, and we confirmed they still match."

That helps developers, operators, and non-technical reviewers alike understand that YAi! is not just hoping its trusted pieces are fine. It is checking.

## 5. What Happens During Normal Development

Most of the time, nothing dramatic happens.

If a developer builds the project without changing any official resource files, the build proceeds normally. No extra ceremony is needed because the existing manifest and signature are still current.

If a developer changes one of the protected official files, the project notices that the trusted packing list is now out of date. At that point, the signing tool needs to refresh the manifest and signature so the official record matches the new approved contents.

In everyday terms, this is less like a security alarm and more like updating the inventory sheet after you intentionally changed what belongs in the box.

## 6. Why the Private Key Stays Separate

The signing key that approves official resources is kept outside the repository in a local secrets location. That private key is not supposed to be committed to source control.

The reason is straightforward: the public key can be shared so anyone can verify; the private key must stay protected so not everyone can pretend to be the signer.

You can think of it like a wax seal stamp. Anyone can look at the seal and inspect it. Not everyone should own the stamp.

## 7. What Happens in CI and Automated Builds

In automated environments such as CI, the normal job is verification, not signing.

That means the pipeline checks whether the committed official files still match the signed manifest. It does not usually create new signatures on the fly. This keeps the automation simpler and safer.

If someone changed a protected file but forgot to refresh the manifest and signature, CI should fail clearly. That failure is useful. It means the system caught a mismatch instead of shipping something ambiguous.

## 8. What Happens If Something Is Wrong

If YAi! finds that a protected file no longer matches the signed manifest, it treats that as a trust problem, not as a tiny detail to ignore.

That could happen because:

- a file was edited on purpose but not re-signed,
- a file was changed accidentally,
- a file is missing,
- or the signed manifest itself is no longer valid.

In those cases, YAi! does not want to guess. It wants to surface the issue clearly so a human can fix it.

This is very aligned with the broader YAi! philosophy in [MANIFEST.md](../../MANIFEST.md): do not fake confidence, do not hide failure, and do not silently trust what has not been verified.

## 9. What This Means for Ordinary Users

If you are not a developer, the main takeaway is simple: YAi! has a way to check whether its built-in trusted ingredients are still authentic.

You do not need to memorize file names or understand digital signatures to understand the purpose. The purpose is to reduce silent tampering, reduce accidental trust, and make the system's starting point more believable.

In plain English, YAi! is trying to say:

"Before I trust my own built-in skills, I make sure they are really the official ones."

## 10. The Bigger Trust Story

This protection model fits the larger YAi! worldview.

YAi! is not trying to be magical. It is trying to be inspectable. It is not trying to look clever by skipping checks. It is trying to behave in a way that sensible people can audit and understand.

That is why built-in skills are protected with signed manifests, why risky actions should cross approval boundaries, and why the project keeps coming back to the same idea: trust over autonomy.

If you want the shortest possible summary, it is this:

YAi! protects its official built-in skills the same way a careful organization protects trusted records: with signatures, verification, and a refusal to quietly ignore mismatches.

## 11. For Technical Readers

If you do want the more technical version later, the underlying implementation still follows the same model as before:

- `public-key.yai.pem` is used for verification.
- `manifest.yai.json` records expected file details.
- `manifest.yai.sig` signs that manifest.
- The private signing key stays outside the repository.
- Runtime trust depends on successful verification.

The difference in this document is only the explanation style. The trust model itself remains the same.
