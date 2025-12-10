# Lookup Experience Manager
Most of the time when a lookup needs to be modified, lookups of the same type most likely need the same updates. It can be cumbersome to find those lookups, the forms they live on, and then have to navigate to each one to make the update. Also needing to find the control on the large forms takes time. For removing the inline New button, the formxml needs to be manually updated and would require the developer to reference API documentation to recall IsInlineNewButton in addition to the steps mentioned prior. 

The Lookup Experience Manager allows the following configurations to be bulk updated for lookup controls without modifying the formxml directly or needing to navigate to each form a control lives on to make the update:
- Inline New Button
- Most Recently Used items
- Main Form Create 
- Main Form Edit 

<img width="1628" height="603" alt="LookupExperienceManager_Screenshot" src="https://github.com/user-attachments/assets/fddf78c5-c859-420e-a4be-2f682a958b4d" />
### Features

- Select any solution → instantly see every lookup pointing to a target entity
- Configure 4 key settings in one place:
  - **Enable/Disable + New button**
  - **Show/Hide Recently Used (MRU)**
  - **Use Main Form dialog for Create**
  - **Use Main Form dialog for Edit**
- Smart **tri-state checkboxes** (Checked / Unchecked / Mixed - if multiple lookups are selected with different configurations)
- Applies changes to **all relevant forms** automatically
- One-click **Save & Publish** (only affected entities)
- Full **refresh** support — reload metadata anytime
- Dark theme UI


### Why You Need This
| Problem                                                | Solved? |
|--------------------------------------------------------|---------|
| Needing to rely on documentation to remove inline +New | YES     |
| +New needing to be disabled on many lookups            | YES     |
| Inconsistent MRU behavior across lookups               | YES     |
| Users opening wrong forms on Create/Edit               | YES     |
| Time wasted clicking through forms                     | YES     |

### Installation

1. Open XrmToolBox > Configuration > Tool Library
2. Search for Lookup Experience Manager
3. Select it and click Install

### Usage

1. Click **Select Solution**
2. Pick a solution
3. Choose a **target entity** (e.g., Account)
4. All lookup controls pointing to it appear
5. Configure settings
6. Click **Save & Publish**

Done.

⚠️ DISCLAIMER — READ BEFORE USING

This is a **community-developed tool** provided "AS IS" with **no warranty**.

- You are **fully responsible** for any changes made to your environment.
- **Always test in a sandbox first**.
- The author is **not liable** for:
  - Data loss
  - Broken forms
  - Solution upgrade issues
  - Managed layer conflicts
  - Any direct or indirect damage

By using this tool, you accept all risk.

### Made For

- Power Platform developers
- Dynamics 365 customizers
- Anyone who hates repetitive form editing

### License

[MIT License](LICENSE) — free for personal and commercial use.

---

**Made with ❤️ for the Power Platform community.**

**By John Cirujales • 2025**

---
