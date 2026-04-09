import React, { useState, useEffect } from 'react';
import { useAuth } from '../context/AuthContext';
import { useNavigate, useLocation } from 'react-router-dom';
import { useTheme } from '../components/common/ThemeContext';
import './Login.css';

const Login = () => {
    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    const [showPassword, setShowPassword] = useState(false);
    const [error, setError] = useState('');
    const [isPending, setIsPending] = useState(false);
    const { login, isAuthenticated } = useAuth();
    const { theme, toggleTheme } = useTheme();
    const navigate = useNavigate();
    const location = useLocation();

    // Redirigir si ya está autenticado
    useEffect(() => {
        if (isAuthenticated) {
            navigate('/control-operativo');
        }
    }, [isAuthenticated, navigate]);

    const handleSubmit = async (e) => {
        e.preventDefault();
        setError('');
        setIsPending(true);

        const result = await login(username, password);

        if (result.success) {
            navigate('/control-operativo');
        } else {
            setError(result.message);
            setIsPending(false);
        }
    };

    return (
        <div className="apple-login-container">
            {/* Visual Background Effects */}
            <div className="apple-visual-background">
                <div className="apple-orb apple-orb-1"></div>
                <div className="apple-orb apple-orb-2"></div>
            </div>

            {/* Selector de Tema Premium */}
            <button 
                onClick={toggleTheme} 
                className="apple-theme-toggle-btn animate-apple-in"
                aria-label="Cambiar tema"
            >
                {theme === 'light' ? (
                    <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor" className="theme-toggle-icon">
                        <path strokeLinecap="round" strokeLinejoin="round" d="M21.752 15.002A9.718 9.718 0 0118 15.75c-5.385 0-9.75-4.365-9.75-9.75 0-1.33.266-2.597.748-3.752A9.753 9.753 0 003 11.25C3 16.635 7.365 21 12.75 21a9.753 9.753 0 009.002-5.998z" />
                    </svg>
                ) : (
                    <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor" className="theme-toggle-icon">
                        <path strokeLinecap="round" strokeLinejoin="round" d="M12 3v2.25m6.364.386l-1.591 1.591M21 12h-2.25m-.386 6.364l-1.591-1.591M12 18.75V21m-4.773-4.227l-1.591 1.591M5.25 12H3m4.227-4.773L5.636 5.636M15.75 12a3.75 3.75 0 11-7.5 0 3.75 3.75 0 017.5 0z" />
                    </svg>
                )}
            </button>

            <div className="apple-login-wrapper animate-apple-in">
                <div className="apple-card login-glass-card">
                    <div className="apple-login-header">
                        <div className="apple-brand-logo">
                            <img src="/favicon.png" alt="ISTPET Logo" className="apple-logo-img" />
                            <div className="apple-brand-text">
                                <span className="apple-title-main">ESCUELA CONDUCCIÓN</span>
                                <span className="apple-title-sub">ISTPET TRAVERSARI</span>
                            </div>
                        </div>
                        <h2 className="apple-login-title">Iniciar sesión</h2>
                    </div>

                    <form onSubmit={handleSubmit} className="apple-login-form">
                        <div className="apple-input-group">
                            <label htmlFor="username">Usuario</label>
                            <div className="apple-field-container">
                                <span className="apple-field-icon">
                                    <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor">
                                        <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 6a3.75 3.75 0 11-7.5 0 3.75 3.75 0 017.5 0zM4.501 20.118a7.5 7.5 0 0114.998 0A17.933 17.933 0 0112 21.75c-2.676 0-5.216-.584-7.499-1.632z" />
                                    </svg>
                                </span>
                                <input
                                    id="username"
                                    type="text"
                                    placeholder="Ej. Admin"
                                    value={username}
                                    onChange={(e) => setUsername(e.target.value)}
                                    required
                                    autoComplete="username"
                                />
                            </div>
                        </div>

                        <div className="apple-input-group">
                            <label htmlFor="password">Contraseña</label>
                            <div className="apple-field-container">
                                <span className="apple-field-icon">
                                    <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor">
                                        <path strokeLinecap="round" strokeLinejoin="round" d="M16.5 10.5V6.75a4.5 4.5 0 10-9 0v3.75m-.75 11.25h10.5a2.25 2.25 0 002.25-2.25v-6.75a2.25 2.25 0 00-2.25-2.25H6.75a2.25 2.25 0 00-2.25 2.25v6.75a2.25 2.25 0 00 2.25 2.25z" />
                                    </svg>
                                </span>
                                <input
                                    id="password"
                                    type={showPassword ? 'text' : 'password'}
                                    placeholder="••••••••"
                                    value={password}
                                    onChange={(e) => setPassword(e.target.value)}
                                    required
                                    autoComplete="current-password"
                                />
                                <button
                                    type="button"
                                    className="apple-password-toggle"
                                    onClick={() => setShowPassword((prev) => !prev)}
                                    aria-label={showPassword ? 'Ocultar contraseña' : 'Mostrar contraseña'}
                                    aria-pressed={showPassword}
                                >
                                    {showPassword ? (
                                        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={1.8} stroke="currentColor" aria-hidden="true">
                                            <path strokeLinecap="round" strokeLinejoin="round" d="M3 3l18 18M10.584 10.587a3 3 0 004.242 4.242m-1.637-5.972A9.956 9.956 0 0112 9c4.478 0 8.268 2.943 9.542 7a10.025 10.025 0 01-4.132 5.411M6.228 6.228A9.956 9.956 0 0112 4c4.478 0 8.268 2.943 9.542 7a9.956 9.956 0 01-1.757 3.196M6.228 6.228L3 3m3.228 3.228A9.956 9.956 0 002.458 11c.56 1.578 1.52 2.99 2.77 4.11" />
                                        </svg>
                                    ) : (
                                        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={1.8} stroke="currentColor" aria-hidden="true">
                                            <path strokeLinecap="round" strokeLinejoin="round" d="M2.458 12C3.732 7.943 7.522 5 12 5s8.268 2.943 9.542 7c-1.274 4.057-5.064 7-9.542 7S3.732 16.057 2.458 12z" />
                                            <path strokeLinecap="round" strokeLinejoin="round" d="M12 15.25a3.25 3.25 0 100-6.5 3.25 3.25 0 000 6.5z" />
                                        </svg>
                                    )}
                                </button>
                            </div>
                        </div>

                        {error && (
                            <div className="apple-error-toast animate-shake">
                                {error}
                            </div>
                        )}

                        <button 
                            type="submit" 
                            className={`btn-apple-primary apple-submit-btn ${isPending ? 'loading' : ''}`}
                            disabled={isPending}
                        >
                            {isPending ? (
                                <div className="apple-spinner"></div>
                            ) : (
                                <>
                                    INGRESAR AL SISTEMA
                                    <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor" className="btn-icon">
                                        <path strokeLinecap="round" strokeLinejoin="round" d="M13.5 4.5L21 12m0 0l-7.5 7.5M21 12H3" />
                                    </svg>
                                </>
                            )}
                        </button>
                    </form>

                </div>

                <div className="apple-page-footer">
                    <p>© {new Date().getFullYear()} ISTPET Traversari • Todos los derechos reservados</p>
                </div>
            </div>
        </div>
    );
};

export default Login;
