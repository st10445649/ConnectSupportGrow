# Connect Support Grow - UI Prototype

Welcome to Connect Support Grow prototype. Built with HTML, CSS (Tailwind), and Alpine.js for interactive components.
This is a responsive UI/UX prototype showcasing design and user flows. The data generated is dummy data, no database has been implemented 
at this stage. 

The main purpose of the prototype is for demonstration and design review.


## Download

### Option 1: Download as ZIP

1. Go to **GitHub page**: https://github.com/st10445649/ConnectSupportGrow
2. Click green **"Code"** button
3. Click **"Download ZIP"**
4. Extract folder to your computer

### Option 2: Clone with Git

```bash
git clone https://github.com/st10445649/ConnectSupportGrow.git
cd ConnectSupportGrow
```

---

## How to View

### Option 1: Open in Visual Studio Code 

1. **Download** the project (see above)
2. **Open VS Code**
3. **Navigate to the project folder** -> ConnectGrow
4. **Run the application** -> dotnet run
5. Open the browser and navigate to https://localhost:----

### Option 2: Open in Visual Studio 2022

1. **Download** the project (see above)
2. **Open Visual Studio 2022**
3. **File** → **Open** → **Project/Solution**
4. Select `ConnectGrow.Web.sln`
5. In Solution Explorer, right-click any `.cshtml` file
6. Click **"View in Browser"**


### Option 3: Run in IIS Express (Windows)

1. **Download** the project
2. **Open Visual Studio 2022**
3. **File** → **Open** 
4. Press **F5** or click **Run** button
5. Application opens in browser at `https://localhost:----`
   

**Note:** Requires .NET 8 SDK installed. 

---

## 📄 Pages Overview

### Public Pages (No Login)

| Page | Path | Description |
|------|------|-------------|
| **Home** | `Views/Home/Index.cshtml` | Landing page with featured webinars |
| **Webinars Browse** | `Views/Webinars/Index.cshtml` | List of all webinars |
| **Webinar Detail** | `Views/Webinars/Detail.cshtml` | Single webinar with details, tabs, FAQ |
| **About** | `Views/Home/About.cshtml` | About company and team |
| **FAQ** | `Views/Home/FAQ.cshtml` | Frequently asked questions |
| **Blog** | `Views/Home/Blog.cshtml` | Blog posts listing |
| **Blog Post** | `Views/Home/BlogDetail.cshtml` | Single blog post view |
| **Login** | `Views/Account/Login.cshtml` | User login form |
| **Register** | `Views/Account/Register.cshtml` | User registration form |
| **Error 404** | `Views/Shared/Error.cshtml` | Page not found |

### User Pages (After Login)

| Page | Path | Description |
|------|------|-------------|
| **Dashboard** | `Views/Dashboard/Index.cshtml` | User main dashboard |
| **Settings** | `Views/Account/Settings.cshtml` | Edit profile, preferences and security settings |
| **Watch Recording** | `Views/Dashboard/WatchRecording.cshtml` | Video player page |
| **Payment Success** | `Views/Bookings/Confirmation.cshtml` | Booking confirmation |

### Admin Pages (Admin Only)

| Page | Path | Description |
|------|------|-------------|
| **Admin Dashboard** | `Areas/Admin/Views/Dashboard/Index.cshtml` | Admin control center |
| **Manage Webinars** | `Areas/Admin/Views/Webinars/Index.cshtml` | List and manage webinars |
| **Create Webinar** | `Areas/Admin/Views/Webinars/Create.cshtml` | Create new webinar |
| **Edit Webinar** | `Areas/Admin/Views/Webinars/Edit.cshtml` | Edit webinar details |
| **Recording Access** | `Areas/Admin/Views/Recordings/Index.cshtml` | Grant recording access |
| **Analytics** | `Areas/Admin/Views/Analytics/Index.cshtml` | View dashboard analytics |
| **Manage Email** | `Areas/Admin/Views/Email/Bulk.cshtml` | Send bulk emails |
| **Manage Blog** | `Areas/Admin/Views/Blog/Index.cshtml` | Manage blog posts |
| **Manage FAQ** | `Areas/Admin/Views/FAQ/Index.cshtml` | Update FAQ entries |

---


## Technologies

### Frontend
- **HTML5** - Semantic markup
- **Tailwind CSS** - Utility-first styling (via CDN)
- **Alpine.js** - Lightweight interactivity (via CDN)
- **Razor Templates** - ASP.NET view engine (`.cshtml` files)

### Design Tools Used
- draw.io - UX journey maps and diagrams
- Canva - Custom logo and icon creation
- Cooolors - Colour palette
- Tailwind CSS documentation


## Requirements

### To View Prototype (Minimum)
- Web browser (Chrome, Firefox, Safari, Edge)
- Downloaded project folder
- Text editor OR VS Code (optional)

### To Run Full Application (Optional)
- .NET 8 SDK
- Visual Studio 2022 or VS Code
- See full setup guide in project

