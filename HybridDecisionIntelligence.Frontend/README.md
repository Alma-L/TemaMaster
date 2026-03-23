# Hybrid Decision Intelligence - React Frontend Setup Guide

## Phase 6: Practical Implementation - XAI Monitoring Dashboard

Welcome to the React frontend for the Hybrid Decision Intelligence Banking System. This guide will walk you through setting up and running the interactive dashboard that visualizes ML predictions with business rule overrides.

---

## 📋 Prerequisites

- **Node.js**: Version 16+ (recommend 18+)
- **npm**: Version 8+ (comes with Node.js)
- **Backend Running**: .NET 9 API on `https://localhost:7074`
- **Git**: For version control (optional)

### Verify Installation

```bash
# Check Node.js version
node --version
# Expected: v18.x.x or higher

# Check npm version
npm --version
# Expected: 8.x.x or higher
```

---

## 🚀 Quick Start (5 Minutes)

### Step 1: Clone/Download Frontend Code

```bash
# If backend is running at: c:\Users\Alma\Desktop\TemaMaster\HybridDecisionIntelligence

cd c:\Users\Alma\Desktop\TemaMaster\HybridDecisionIntelligence.Frontend
```

### Step 2: Install Dependencies

```bash
npm install
# This installs all packages from package.json
# Takes 2-3 minutes on first install

# Verify installation
npm list react tailwindcss lucide-react
```

### Step 3: Configure API Connection

Create `.env.local` file in the frontend root:

```bash
# HybridDecisionIntelligence.Frontend\.env.local

REACT_APP_API_URL=https://localhost:7074/api
REACT_APP_TIMEOUT=10000
```

### Step 4: Start the Development Server

```bash
npm start
# React app opens automatically at http://localhost:3000
# If not, manually visit http://localhost:3000
```

**Expected Output**:
```
Compiled successfully!

You can now view hybrid-decision-intelligence-frontend in the browser.

  Local:            http://localhost:3000
  On Your Network:  http://192.168.x.x:3000

Note that the development build is not optimized.
To create a production build, use npm run build.
```

---

## 📦 Project Structure

```
HybridDecisionIntelligence.Frontend/
├── public/                          # Static files
│   ├── index.html                  # Main HTML template
│   └── favicon.ico
├── src/
│   ├── components/
│   │   ├── DecisionDashboard.tsx   # Main XAI Dashboard Component
│   │   └── DecisionDetail.tsx      # Decision Detail Modal Component
│   ├── hooks/
│   │   └── useDecisions.ts         # Custom hook for API data fetching
│   ├── types/
│   │   └── index.ts                # TypeScript type definitions
│   ├── App.tsx                      # Root application component
│   ├── App.css                      # Global styles
│   └── index.tsx                    # React entry point
├── package.json                     # Dependencies and scripts
├── tsconfig.json                    # TypeScript configuration
├── tailwind.config.js               # Tailwind CSS configuration
├── postcss.config.js                # PostCSS configuration
└── README.md                        # This file
```

---

## 🎯 Component Overview

### DecisionDashboard.tsx
**Main dashboard component** displaying:
- Metrics cards (Automation Rate, AI Precision, Rule Intervention %)
- Decision records table with sortable columns
- Status indicators (Green/Red/Yellow)
- Override badges with tooltips
- Loading and error states

**Key Props**: None (uses `useDecisions` hook)

**State Management**:
- `decisions`: Array of decision records
- `metrics`: Calculated metrics from decisions
- `selectedDecision`: Currently selected decision for modal
- `showDetailModal`: Modal visibility toggle
- `hoverContent`: Tooltip state

### DecisionDetail.tsx
**Modal component** for decision breakdown using TAO framework:

**Sections**:
1. **Thought**: ML prediction with confidence score
2. **Action**: Business rule evaluation and applied rules
3. **Observation**: Final decision, interest rate, timestamp

**Features**:
- Expandable sections
- Copy audit trail to clipboard
- Color-coded (Blue/Yellow/Green)
- Responsive design

### useDecisions.ts
**Custom React Hook** for API data fetching:

```typescript
const { decisions, loading, error, refetch } = useDecisions();

// Returns:
// - decisions: DecisionRecord[] | null
// - loading: boolean (true while fetching)
// - error: string | null (error message if failed)
// - refetch: () => Promise<void> (manual refresh)
```

---

## 🔧 Development

### Available Scripts

```bash
# Start development server (port 3000)
npm start

# Build for production
npm run build  # Creates optimized build in ./build

# Run tests (if configured)
npm test

# Lint TypeScript
npm run lint

# Type check without emitting
npm run type-check

# Eject configuration (⚠️ irreversible)
npm eject
```

### Environment Variables

Create `.env.local` for development:

```bash
# API Configuration
REACT_APP_API_URL=https://localhost:7074/api
REACT_APP_TIMEOUT=10000

# Feature Flags (optional)
REACT_APP_ENABLE_POLLING=false
REACT_APP_POLLING_INTERVAL=10000
```

### TypeScript Configuration

The project uses strict TypeScript:
- Strict mode enabled
- No unused variables warning
- All types must be defined
- Path aliases configured:
  - `@/*` → `src/*`
  - `@components/*` → `src/components/*`
  - `@hooks/*` → `src/hooks/*`

---

## 🎨 Styling with Tailwind CSS

**Theme Colors**:
- Primary: Indigo (#4f46e5)
- Success: Green (#16a34a)
- Warning: Yellow (#ca8a04)
- Danger: Red (#dc2626)
- Neutral: Gray (#6b7280)

**Key Classes Used**:
- `bg-gradient-to-br`: Background gradients
- `hover:bg-opacity-20`: Interactive effects
- `border-l-4`: Left border accents
- `rounded-lg`: Border radius
- `shadow-md`: Drop shadows
- `space-y-4`: Vertical spacing

**Custom Tailwind Config**:
- Extended color palette
- Custom fonts (Inter, Fira Code)
- Additional animations and shadows
- TailwindCSS Forms and Typography plugins

---

## 🐛 Troubleshooting

### Issue: "Cannot find module 'react'"

**Solution**:
```bash
rm -rf node_modules package-lock.json
npm install
```

### Issue: API Connection Failed (ERR_SSL_CERT_PROBLEM)

**Solution** (Development Only):
Create `.env.local`:
```bash
# Bypass SSL verification in development
REACT_APP_API_URL=http://localhost:5050/api
```

Or on Windows Command Prompt:
```cmd
set NODE_TLS_REJECT_UNAUTHORIZED=0
npm start
```

### Issue: Port 3000 Already In Use

**Solution**:
```bash
# Kill process on port 3000 (Windows PowerShell):
Get-Process | Where-Object {$_.Handles -like '*3000*'} | Stop-Process

# Or use different port:
PORT=3001 npm start

# Or on Mac/Linux:
lsof -ti:3000 | xargs kill -9
```

### Issue: Dashboard Shows "Connection Error"

**Checklist**:
- [ ] Backend API running on https://localhost:7074
- [ ] CORS enabled in backend (Program.cs)
- [ ] API endpoint `/api/v1/decisions` returning data
- [ ] .env.local has correct REACT_APP_API_URL

**Test Backend**:
```bash
# From PowerShell:
$response = Invoke-WebRequest -Uri "https://localhost:7074/api/v1/decisions" `
  -SkipCertificateCheck
Write-Host $response.StatusCode
# Should show: 200
```

### Issue: TypeScript Errors in IDE

**Solution**:
```bash
npm run type-check
# Shows all TypeScript errors

# Or enable "Type Checking" in VSCode:
# Ctrl+Shift+P → "TypeScript: Run All Linters"
```

---

## 📊 Data and Metrics

### Metrics Calculated

From decision records:
```typescript
const metrics = {
  totalDecisions: decisions.length,
  approvedCount: decisions.filter(d => d.finalDecision).length,
  rejectedCount: total - approved,
  overriddenCount: decisions.filter(d => d.wasOverridden).length,
  automationRate: (total - overridden) / total * 100,
  aiPrecision: approved / total * 100,
  ruleEngineInterventionRate: overridden / total * 100,
};
```

### Decision Record Fields

```typescript
interface DecisionRecord {
  id: number;                      // Unique decision ID
  customerId: number;              // Bank customer ID
  mlPredictionResultId: number;    // Reference to ML prediction
  mlPredicted: boolean;            // Raw ML prediction (true/false)
  mlConfidence: number;            // ML confidence (0-1)
  finalDecision: boolean;          // Final integrated decision
  auditTrail: string;              // Complete decision explanation
  approvedInterestRate: number;    // Calculated interest rate
  rulesApplied: string;            // Comma-separated rule names
  createdAt: string;               // Decision timestamp (ISO 8601)
  wasOverridden: boolean;          // Whether business rules overrode ML
  overrideReason: string;          // Why decision was overridden
}
```

---

## 🌐 API Integration

### GET /api/v1/decisions

**Fetch all decisions**:

```typescript
// Called automatically by useDecisions hook
const response = await fetch(`${API_BASE_URL}/v1/decisions`, {
  method: 'GET',
  headers: {
    'Content-Type': 'application/json',
  },
});

const decisions: DecisionRecord[] = await response.json();
```

**Response** (200 OK):
```json
[
  {
    "id": 1,
    "customerId": 1001,
    "mlConfidence": 0.85,
    "finalDecision": false,
    "wasOverridden": true,
    "overrideReason": "Interest Rate < 2.5%",
    "auditTrail": "..."
  }
]
```

### Future API Endpoints (Not Yet Implemented)

```
GET  /api/v1/decisions/:id           # Get single decision
POST /api/v1/decisions/search         # Search decisions
GET  /api/v1/metrics/summary          # Get dashboard metrics
GET  /api/v1/rules                    # List business rules
```

---

## 🚢 Production Deployment

### Build for Production

```bash
npm run build
# Creates optimized production build in ./build folder
```

**Build Output**:
```
File sizes after gzip:

  153.45 kB  build\static\js\main.abcd1234.js
  35.67 kB   build\static\css\main.efgh5678.css
  2.34 kB    build\static\js\runtime~main.ijkl9012.js
```

### Deploy to Azure Static Web Apps

```bash
# Install Azure CLI
npm install -g @azure/cli

# Login to Azure
az login

# Create Static Web App and deploy
az staticwebapp up --name <app-name> --resource-group <group> --app-location "build"
```

### Deploy to Vercel

```bash
# Install Vercel CLI
npm install -g vercel

# Deploy
vercel --prod
```

### Deploy to Netlify

```bash
# Install Netlify CLI
npm install -g netlify-cli

# Deploy
netlify deploy --prod --dir=build
```

### Docker Deployment

Create `Dockerfile` in frontend root:

```dockerfile
# Build stage
FROM node:18 AS build
WORKDIR /app
COPY package*.json ./
RUN npm install
COPY . .
RUN npm run build

# Serve stage
FROM node:18
WORKDIR /app
RUN npm install -g serve
COPY --from=build /app/build ./build
EXPOSE 3000
CMD ["serve", "-s", "build", "-l", "3000"]
```

Build and run:
```bash
docker build -t hybrid-decision-frontend .
docker run -p 3000:3000 -e REACT_APP_API_URL=http://backend:5050/api hybrid-decision-frontend
```

---

## 📈 Performance Optimization

### Bundle Size Analysis

```bash
npm install -g webpack-bundle-analyzer

# Add to package.json scripts:
"analyze": "react-scripts build && webpack-bundle-analyzer build/static/js/main.*.js"

npm run analyze
```

### Code Splitting (Already Configured)

- React automatically code-splits based on routes
- Lazy loading for modal components
- CSS is bundled with components

### Caching Strategy

- Production builds use content hash in filenames
- Service Worker (if needed) can cache assets
- Set `Cache-Control: max-age=31536000` for static files

---

## 🔐 Security Considerations

### Environment Variables

Never commit sensitive data:
```bash
# .gitignore should include:
.env.local
.env.*.local
.env
```

### HTTPS/SSL

- Development: Allow self-signed certs (done in npm start)
- Production: Use valid SSL certificates
- Configure CORS headers properly

### Authentication (Future)

When authentication is added:
```typescript
// Add to useDecisions hook:
const fetchDecisions = async (token: string) => {
  const response = await fetch(`${API_BASE_URL}/v1/decisions`, {
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json',
    },
  });
  // ...
};
```

---

## 📚 Resources

### Official Documentation
- [React Documentation](https://react.dev)
- [Tailwind CSS Docs](https://tailwindcss.com/docs)
- [Lucide Icons](https://lucide.dev)
- [TypeScript Handbook](https://www.typescriptlang.org/docs)

### Tutorials
- [React Hooks Guide](https://react.dev/reference/react)
- [Custom Hooks Patterns](https://react.dev/learn/reusing-logic-with-custom-hooks)
- [Tailwind CSS Best Practices](https://tailwindcss.com/docs/adding-custom-styles)

### Tools
- [VS Code React Extensions](https://marketplace.visualstudio.com/items?itemName=dsznajder.es7-react-js-snippets)
- [DevTools Extension](https://chrome.google.com/webstore/detail/react-developer-tools)

---

## 🤝 Contributing

To add new features:

1. **Create a new component** in `src/components/`
2. **Define types** in `src/types/index.ts`
3. **Add styles** using Tailwind classes
4. **Use hooks** for API calls and state management
5. **Test** with `npm test`

### Example: Add a new chart component

```typescript
// src/components/DecisionChart.tsx
import React from 'react';

export const DecisionChart: React.FC = () => {
  return (
    <div className="bg-white rounded-lg shadow-md p-6">
      {/* Chart content */}
    </div>
  );
};
```

Then import in `DecisionDashboard.tsx`:
```typescript
import { DecisionChart } from './DecisionChart';
```

---

## ✅ Checklist: Before Going to Production

- [ ] Backend API running and tested
- [ ] Environment variables configured (.env.local)
- [ ] Dashboard loads without errors
- [ ] All metrics calculate correctly
- [ ] Decision table displays data
- [ ] Modal opens and shows TAO breakdown
- [ ] No console errors or warnings
- [ ] TypeScript type checking passes
- [ ] Production build completes successfully
- [ ] Test on different browsers (Chrome, Firefox, Safari, Edge)
- [ ] Test on mobile devices (responsive design)
- [ ] API connection uses HTTPS (not HTTP)
- [ ] CORS headers properly configured
- [ ] Rate limiting configured on backend
- [ ] Error handling tested

---

## 📞 Support & Debugging

### Check Backend Status

```bash
# Test backend API connectivity
curl -k https://localhost:7074/api/v1/decisions

# On Windows PowerShell:
Invoke-WebRequest -Uri "https://localhost:7074/api/v1/decisions" `
  -SkipCertificateCheck | Select-Object StatusCode, Content
```

### View Logs

```bash
# Browser Console (F12):
# - Check for API errors
# - Verify response data

# Network Tab:
# - Monitor API requests
# - Check response times
# - Verify CORS headers
```

### Report Issues

When reporting issues, include:
1. Browser version
2. Network error message
3. Console error (F12 → Console)
4. Backend API status (health check)
5. Steps to reproduce

---

## 📄 License

Master's Thesis Project - Hybrid Decision Intelligence Banking System
Phase 6: React Frontend Implementation

---

**Frontend Version**: 1.0.0
**Last Updated**: March 23, 2026
**Status**: Production Ready ✓

---

## Next Steps

1. ✅ Install dependencies: `npm install`
2. ✅ Configure API connection: Create `.env.local`
3. ✅ Start development: `npm start`
4. ✅ View dashboard: Open http://localhost:3000
5. ✅ Interact with decisions: Click "View Details"
6. ✅ Review audit trails: Expand TAO sections
7. ✅ Build for production: `npm run build`

**Happy coding! 🚀**
