export const environment = {
  production: true,

  // config for Azure
  apiClientUrl: 'https://eduflex-api.proudground-29bd75df.australiaeast.azurecontainerapps.io',
  publicApiUrl: 'https://eduflex-api.proudground-29bd75df.australiaeast.azurecontainerapps.io',

  //config for aws
  // apiClientUrl: 'http://eduflex-api-alb-990547956.ap-southeast-2.elb.amazonaws.com',
  // publicApiUrl: 'http://eduflex-api-alb-990547956.ap-southeast-2.elb.amazonaws.com',
  // Google reCAPTCHA v2 "I'm not a robot" site key ("Edu captcha" site, registered for
  // eduflex.net.au + www.eduflex.net.au). Secret lives in Azure Key Vault as recaptcha-secret.
  recaptchaSiteKey: '6Ld5J24tAAAAAHMXdmr8L9tGn2IH8zutABE_5UA7'
};
