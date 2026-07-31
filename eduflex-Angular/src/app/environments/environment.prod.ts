export const environment = {
  production: true,

  // config for Azure
  apiClientUrl: 'https://eduflex-api.proudground-29bd75df.australiaeast.azurecontainerapps.io',
  publicApiUrl: 'https://eduflex-api.proudground-29bd75df.australiaeast.azurecontainerapps.io',

  //config for aws
  // apiClientUrl: 'http://eduflex-api-alb-990547956.ap-southeast-2.elb.amazonaws.com',
  // publicApiUrl: 'http://eduflex-api-alb-990547956.ap-southeast-2.elb.amazonaws.com',
  // Google reCAPTCHA v2 "I'm not a robot" site key — same "test" reCAPTCHA site as dev.
  // Add your production domain (e.g. proudground-29bd75df.australiaeast.azurecontainerapps.io)
  // to this site's domain list at google.com/recaptcha/admin before deploying, or the widget will fail there.
  recaptchaSiteKey: '6LcuGVgtAAAAALe6GJBR49pa6cvclOJg8gYqwZS'
};
