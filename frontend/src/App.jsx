import React from 'react';
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import Home from './pages/Home';
import Students from './pages/Students';
import Vehicles from './pages/Vehicles';
import ControlOperativo from './pages/ControlOperativo';

/**
 * App Root - ISTPET 2026
 * Monitoring: Routes & Context Integrity
 */
function App() {
  console.log('[APP] Inicializando sistema Zenith ISTPET 2026...');

  return (
    <BrowserRouter>
      {/* Test de Seguridad: Si ves este mensaje, React está funcionando */}
      <div className="fixed bottom-4 right-4 z-[999] opacity-20 pointer-events-none">
          <p className="text-[8px] font-bold text-slate-400 uppercase tracking-widest">ISTPET_CORE_OK</p>
      </div>

      <div className="min-h-screen w-full overflow-x-hidden bg-[var(--apple-bg)]">
        <Routes>
          <Route path="/" element={<Home />} />
          <Route path="/estudiantes" element={<Students />} />
          <Route path="/vehiculos" element={<Vehicles />} />
          <Route path="/logistica" element={<ControlOperativo />} />
          <Route path="*" element={<Home />} />
        </Routes>
      </div>
    </BrowserRouter>
  );
}

export default App;
