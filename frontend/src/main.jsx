import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.jsx'
import { ThemeProvider } from './components/common/ThemeContext.jsx'
import { ToastProvider } from './context/ToastContext.jsx'
import { OperativeAlertsProvider } from './context/OperativeAlertsContext.jsx'

if ('serviceWorker' in navigator) {
  window.addEventListener('load', () => {
    navigator.serviceWorker.register('/sw.js').catch((error) => {
      console.error('No se pudo registrar el service worker:', error)
    })
  })
}

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
