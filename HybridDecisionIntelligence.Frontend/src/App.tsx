import DecisionDashboard from './components/DecisionDashboard';
import { TooltipProvider } from 'src/components/ui/tooltip';
import './App.css';

/**
 * Main Application Component
 * Phase 6: React Frontend for XAI Monitoring Dashboard
 */
function App() {
  return (
    <TooltipProvider delayDuration={150}>
      <div className="App">
        <DecisionDashboard />
      </div>
    </TooltipProvider>
  );
}

export default App;
