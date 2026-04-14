/** @returns {boolean} */
export function systemNotificationsSupported() {
    return typeof window !== 'undefined' && 'Notification' in window;
}

/** @returns {'default' | 'granted' | 'denied'} */
export function getSystemNotificationPermission() {
    if (!systemNotificationsSupported()) return 'denied';
    return Notification.permission;
}

/**
 * Debe llamarse tras un gesto del usuario (tap) para cumplir políticas del navegador.
 * @returns {Promise<'default' | 'granted' | 'denied'>}
 */
export async function requestSystemNotificationPermission() {
    if (!systemNotificationsSupported()) return 'denied';
    try {
        return await Notification.requestPermission();
    } catch {
        return 'denied';
    }
}

/**
 * Muestra una notificación del sistema si hay permiso concedido.
 * `tag` evita apilar varias iguales (la nueva reemplaza la anterior con el mismo tag).
 */
export function showSystemNotification({ title, body, tag = 'istpet-alert' }) {
    if (!systemNotificationsSupported() || Notification.permission !== 'granted') return;

    try {
        const icon = typeof window !== 'undefined' ? `${window.location.origin}/favicon.png` : '/favicon.png';
        const n = new Notification(title, {
            body,
            tag: String(tag),
            icon,
            badge: icon,
            lang: 'es-EC'
        });
        n.onclick = () => {
            try {
                window.focus();
            } catch {
                /* noop */
            }
            n.close();
        };
    } catch (e) {
        console.warn('[browserNotifications]', e);
    }
}
