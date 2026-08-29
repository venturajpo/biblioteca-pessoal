/**
 * Configuração usada em `ng serve`.
 * O caminho relativo `/api` é resolvido pelo proxy do dev-server (proxy.conf.json),
 * que encaminha para a API rodando em http://localhost:5261.
 */
export const environment = {
  producao: false,
  urlDaApi: '/api',
};
