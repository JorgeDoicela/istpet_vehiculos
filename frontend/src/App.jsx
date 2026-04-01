import React from 'react';
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import Home from './pages/Home';
import Students from './pages/Students';
import Vehicles from './pages/Vehicles';

function App() {
  return (
    <BrowserRouter>
      <div className="min-h-screen w-full overflow-x-hidden">
        <Routes>
          <Route path="/" element={<Home />} />
          <Route path="/estudiantes" element={<Students />} />
          <Route path="/vehiculos" element={<Vehicles />} />
        </Routes>
      </div>
    </BrowserRouter>
  );
}

export default App;
