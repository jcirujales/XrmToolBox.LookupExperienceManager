# Lookup Experience Manager

**Bulk configure lookup control behavior across ALL forms in a solution — in seconds.**

The Lookup Experience Manager allows the following configurations to be bulk updated for lookup controls without modifying the formxml directly or needing to navigate to each form a control lives on to make the update:
- Inline New Button
- Most Recently Used items
- Main Form Create 
- Main Form Edit 

On lookup controls, instead of manually disabling "+ New", hiding MRU, or updating main form dialogs on batches of lookup fields.

This plugin does it **all at once**.

![image]([https://github.com/jcirujales/XrmToolBox.LookupExperienceManager/blob/master/BulkLookupConfiuration.XrmToolBoxTool/Images/LookupExperienceManager_Screenshot.png](https://github.com/jcirujales/XrmToolBox.LookupExperienceManager/blob/master/XrmToolBox.LookupExperienceManager/Images/LookupExperienceManager_Screenshot.png))

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
Most of the time when a lookup needs to be modified, lookups of the same type most likely need the same updates. It can be cumbersome to find those lookups, the forms they live on, and then have to navigate to each one to make the update. Also needing to find the control on the large forms takes time. For removing the inline New button, the formxml needs to be manually updated and would require the developer to reference API documentation to recall IsInlineNewButton in addition to the steps mentioned prior. 

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
