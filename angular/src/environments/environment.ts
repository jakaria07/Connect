import { Environment } from '@abp/ng.core';

const baseUrl = 'http://localhost:4200';

const oAuthConfig = {
  issuer: 'https://localhost:44355/',
  redirectUri: baseUrl,
  clientId: 'Connect_App',
  responseType: 'code',
  scope: 'offline_access Connect',
  requireHttps: true,
};

export const environment = {
  production: false,
  application: {
    baseUrl,
    name: 'Connect',
  },
  oAuthConfig,
  apis: {
    default: {
      url: 'https://localhost:44355',
      rootNamespace: 'Wafi.Connect',
    },
    AbpAccountPublic: {
      url: oAuthConfig.issuer,
      rootNamespace: 'AbpAccountPublic',
    },
  },
} as Environment;
