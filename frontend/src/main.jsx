import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.jsx'
import { ThemeProvider } from './components/common/ThemeContext.jsx'
import { ToastProvider } from './context/ToastContext.jsx'
import { OperativeAlertsProvider } from './context/OperativeAlertsContext.jsx'

createRoot(document.getElementById('root')).render(
  <StrictMode>
    <ThemeProvider>
      <ToastProvider>
        <OperativeAlertsProvider>
          <App />
        </OperativeAlertsProvider>
      </ToastProvider>
    </ThemeProvider>
  </StrictMode>,
)
