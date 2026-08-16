export const environment = {
  production: true,

  // config for Azure
  apiClientUrl: 'https://eduflex-api.proudground-29bd75df.australiaeast.azurecontainerapps.io',
  publicApiUrl: 'https://eduflex-api.proudground-29bd75df.australiaeast.azurecontainerapps.io',

  //config for aws
  // apiClientUrl: 'http://eduflex-api-alb-990547956.ap-southeast-2.elb.amazonaws.com',
  // publicApiUrl: 'http://eduflex-api-alb-990547956.ap-southeast-2.elb.amazonaws.com',
  // Google reCAPTCHA v2 "I'm not a robot" site key ("Eduflex Captcha" site — same key used
  // locally, registered for localhost + eduflex.net.au + www.eduflex.net.au). Matching secret
  // lives in Container App secret `recaptcha-secret` on eduflex-api.
  recaptchaSiteKey: '6LcuGVgtAAAAAALe6GJBR49pa6cvcIOJg8gYqwZS',
};
