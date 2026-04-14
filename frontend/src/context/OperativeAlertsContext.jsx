import React, {
    createContext,
    useContext,
    useState,
    useCallback,
    useMemo,
    useRef,
    useEffect
} from 'react';
import { useToast } from './ToastContext';
import { minutosEnRuta } from '../utils/agendaUi';
import {
    systemNotificationsSupported,
    getSystemNotificationPermission,
    requestSystemNotificationPermission,
    showSystemNotification
} from '../utils/browserNotifications';

const OperativeAlertsContext = createContext(null);

const LIMITE_MINUTOS_EN_RUTA = 120;

export function OperativeAlertsProvider({ children }) {
    const { warning } = useToast();
    const [clasesActivas, setClasesActivas] = useState([]);
    const [tick, setTick] = useState(0);
    const [systemNotifPermission, setSystemNotifPermission] = useState(() =>
        getSystemNotificationPermission()
    );
    const notificadosRef = useRef(new Set());

    const requestSystemNotifications = useCallback(async () => {
        await requestSystemNotificationPermission();
        const p = getSystemNotificationPermission();
        setSystemNotifPermission(p);
        return p;
    }, []);

    useEffect(() => {
        const sync = () => setSystemNotifPermission(getSystemNotificationPermission());
        window.addEventListener('focus', sync);
        return () => window.removeEventListener('focus', sync);
    }, []);

    useEffect(() => {
        const onVis = () => {
            if (document.visibilityState === 'visible') {
                setSystemNotifPermission(getSystemNotificationPermission());
            }
        };
        document.addEventListener('visibilitychange', onVis);
        return () => document.removeEventListener('visibilitychange', onVis);
    }, []);

    useEffect(() => {
        const id = setInterval(() => setTick((t) => t + 1), 30000);
        return () => clearInterval(id);
    }, []);

    const publishClasesActivas = useCallback((list) => {
        setClasesActivas(Array.isArray(list) ? list : []);
    }, []);

    const alertasExcesoRuta = useMemo(() => {
        void tick;
        const out = [];
        for (const c of clasesActivas) {
            const m = minutosEnRuta(c.salida);
            if (m != null && m >= LIMITE_MINUTOS_EN_RUTA) {
                out.push({ ...c, minutosEnRuta: m });
            }
        }
        return out;
    }, [clasesActivas, tick]);

    useEffect(() => {
        const activos = new Set(alertasExcesoRuta.map((a) => a.idPractica));
        for (const id of [...notificadosRef.current]) {
            if (!activos.has(id)) notificadosRef.current.delete(id);
        }
        for (const a of alertasExcesoRuta) {
            const id = a.idPractica;
            if (notificadosRef.current.has(id)) continue;
            notificadosRef.current.add(id);
            const veh =
                a.numeroVehiculo != null && String(a.numeroVehiculo).trim() !== ''
                    ? `#${a.numeroVehiculo}`
                    : 'Vehículo';
            const placa = (a.placa || '').trim();
            const sufijo = placa ? ` (${placa})` : '';
            const alumno = (a.estudiante || '').trim();
            const msg = `${veh}${sufijo}: lleva más de 2 h en ruta${alumno ? ` — ${alumno}` : ''}.`;
            warning(msg, 9000);
            showSystemNotification({
                title: 'ISTPET — Ruta prolongada',
                body: msg,
                tag: `istpet-ruta-larga-${id}`
            });
        }
    }, [alertasExcesoRuta, warning]);

    const value = useMemo(
        () => ({
            publishClasesActivas,
            alertasExcesoRuta,
            limiteMinutosEnRuta: LIMITE_MINUTOS_EN_RUTA,
            systemNotificationsSupported: systemNotificationsSupported(),
            systemNotifPermission,
            requestSystemNotifications
        }),
        [
            publishClasesActivas,
            alertasExcesoRuta,
            systemNotifPermission,
            requestSystemNotifications
        ]
    );

    return (
        <OperativeAlertsContext.Provider value={value}>
            {children}
        </OperativeAlertsContext.Provider>
    );
}

export function useOperativeAlerts() {
    const ctx = useContext(OperativeAlertsContext);
    if (!ctx) {
        throw new Error('useOperativeAlerts debe usarse dentro de OperativeAlertsProvider');
    }
    return ctx;
}
