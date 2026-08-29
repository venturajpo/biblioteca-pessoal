/**
 * Configuração usada nas builds de produção.
 * O caminho relativo `/api` é resolvido pelo nginx, que encaminha para o contêiner da API.
 */
export const environment = {
  producao: true,
  urlDaApi: '/api',
};
