import api from './api';

const reportService = {
    getReportePracticas: async (filtros = {}) => {
        try {
            const params = new URLSearchParams();
            if (filtros.fechaInicio) params.append('fechaInicio', filtros.fechaInicio);
            if (filtros.fechaFin) params.append('fechaFin', filtros.fechaFin);
            if (filtros.instructorId) params.append('instructorId', filtros.instructorId);

            const response = await api.get(`/reports/practicas?${params.toString()}`);
            return response.data;
        } catch (error) {
            throw error.response?.data || { message: 'Error connection to server' };
        }
    }
};

export default reportService;
