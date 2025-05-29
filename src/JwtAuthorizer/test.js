import { handler } from './index.js';
import { SignJWT } from 'jose';

// Mock environment variables
process.env.JWT_ISSUER = 'https://test.auth.example.com';
process.env.JWT_AUDIENCE = 'test-audience';
process.env.JWT_SIGNING_KEY = 'test-signing-key';

const signingKey = new TextEncoder().encode(process.env.JWT_SIGNING_KEY);

async function generateTestToken(payload = {}) {
  return new SignJWT(payload)
    .setProtectedHeader({ alg: 'HS256' })
    .setIssuedAt()
    .setIssuer(process.env.JWT_ISSUER)
    .setAudience(process.env.JWT_AUDIENCE)
    .setExpirationTime('1h')
    .sign(signingKey);
}

async function runTests() {
  console.log('Running JWT Authorizer tests...');

  // Test 1: Valid token
  const validToken = await generateTestToken({ sub: 'test-user' });
  const validResult = await handler({
    authorizationToken: `Bearer ${validToken}`,
    methodArn: 'arn:aws:execute-api:us-east-1:123456789012:api/test/GET/test'
  });
  console.assert(validResult.policyDocument.Statement[0].Effect === 'Allow', 'Valid token should be allowed');
  console.assert(validResult.principalId === 'test-user', 'Principal ID should match token subject');

  // Test 2: Invalid token
  const invalidResult = await handler({
    authorizationToken: 'Bearer invalid.token.here',
    methodArn: 'arn:aws:execute-api:us-east-1:123456789012:api/test/GET/test'
  });
  console.assert(invalidResult.policyDocument.Statement[0].Effect === 'Deny', 'Invalid token should be denied');

  // Test 3: Missing token
  const missingResult = await handler({
    authorizationToken: '',
    methodArn: 'arn:aws:execute-api:us-east-1:123456789012:api/test/GET/test'
  });
  console.assert(missingResult.policyDocument.Statement[0].Effect === 'Deny', 'Missing token should be denied');

  // Test 4: Expired token
  const expiredToken = await new SignJWT({ sub: 'test-user' })
    .setProtectedHeader({ alg: 'HS256' })
    .setIssuedAt()
    .setIssuer(process.env.JWT_ISSUER)
    .setAudience(process.env.JWT_AUDIENCE)
    .setExpirationTime('1s')
    .sign(signingKey);
  
  // Wait for token to expire
  await new Promise(resolve => setTimeout(resolve, 2000));
  
  const expiredResult = await handler({
    authorizationToken: `Bearer ${expiredToken}`,
    methodArn: 'arn:aws:execute-api:us-east-1:123456789012:api/test/GET/test'
  });
  console.assert(expiredResult.policyDocument.Statement[0].Effect === 'Deny', 'Expired token should be denied');

  console.log('All tests completed!');
}

runTests().catch(console.error); 