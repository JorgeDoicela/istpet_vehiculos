import React, { useState, useEffect } from 'react';
import { useAuth } from '../context/AuthContext';
import { useNavigate } from 'react-router-dom';
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
                                <input
                                    id="username"
                                    type="text"
                                    placeholder="Ej. Admin"
                                    className="no-icon-input"
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
                                <input
                                    id="password"
                                    type={showPassword ? 'text' : 'password'}
                                    placeholder="••••••••"
                                    className="no-icon-input"
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
                                            <path strokeLinecap="round" strokeLinejoin="round" d="M3.98 8.223A10.477 10.477 0 001.934 12C3.226 16.338 7.244 19.5 12 19.5c.993 0 1.953-.138 2.863-.395M6.228 6.228A10.451 10.451 0 0112 4.5c4.756 0 8.773 3.162 10.065 7.498a10.522 10.522 0 01-4.293 5.774M6.228 6.228 3 3m3.228 3.228 3.65 3.65m7.894 7.894L21 21m-3.228-3.228-3.65-3.65m0 0a3 3 0 10-4.243-4.243m4.242 4.242L9.88 9.88" />
                                        </svg>
                                    ) : (
                                        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={1.8} stroke="currentColor" aria-hidden="true">
                                            <path strokeLinecap="round" strokeLinejoin="round" d="M2.036 12.322a1.012 1.012 0 010-.639C3.423 7.51 7.36 4.5 12 4.5c4.638 0 8.573 3.007 9.963 7.178.07.207.07.431 0 .639C20.577 16.49 16.64 19.5 12 19.5c-4.638 0-8.573-3.007-9.963-7.178Z" />
                                            <path strokeLinecap="round" strokeLinejoin="round" d="M15 12a3 3 0 11-6 0 3 3 0 016 0Z" />
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
