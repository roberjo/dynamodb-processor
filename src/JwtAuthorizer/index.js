import { jwtVerify } from 'jose';

// Cache environment variables to avoid repeated lookups
const JWT_ISSUER = process.env.JWT_ISSUER;
const JWT_AUDIENCE = process.env.JWT_AUDIENCE;
const JWT_SIGNING_KEY = new TextEncoder().encode(process.env.JWT_SIGNING_KEY);

// Pre-compile the policy template for better performance
const POLICY_TEMPLATE = {
  principalId: '',
  policyDocument: {
    Version: '2012-10-17',
    Statement: [{
      Action: 'execute-api:Invoke',
      Effect: '',
      Resource: ''
    }]
  }
};

/**
 * Generates an IAM policy document
 * @param {string} principalId - The principal ID
 * @param {string} effect - The effect (Allow/Deny)
 * @param {string} resource - The resource ARN
 * @returns {Object} The policy document
 */
function generatePolicy(principalId, effect, resource) {
  const policy = { ...POLICY_TEMPLATE };
  policy.principalId = principalId;
  policy.policyDocument.Statement[0].Effect = effect;
  policy.policyDocument.Statement[0].Resource = resource;
  return policy;
}

/**
 * Extracts the JWT token from the Authorization header
 * @param {string} authHeader - The Authorization header
 * @returns {string|null} The extracted token or null
 */
function extractToken(authHeader) {
  if (!authHeader?.startsWith('Bearer ')) {
    return null;
  }
  return authHeader.slice(7);
}

/**
 * Validates the JWT token
 * @param {string} token - The JWT token to validate
 * @returns {Promise<Object|null>} The decoded token or null
 */
async function validateToken(token) {
  try {
    const { payload } = await jwtVerify(token, JWT_SIGNING_KEY, {
      issuer: JWT_ISSUER,
      audience: JWT_AUDIENCE,
      algorithms: ['HS256']
    });
    return payload;
  } catch (error) {
    return null;
  }
}

/**
 * Lambda function handler
 * @param {Object} event - The API Gateway authorizer event
 * @returns {Promise<Object>} The IAM policy document
 */
export const handler = async (event) => {
  try {
    // Extract token from Authorization header
    const token = extractToken(event.authorizationToken);
    if (!token) {
      return generatePolicy('user', 'Deny', event.methodArn);
    }

    // Validate token
    const payload = await validateToken(token);
    if (!payload) {
      return generatePolicy('user', 'Deny', event.methodArn);
    }

    // Extract user ID from claims
    const userId = payload.sub || 'user';

    // Generate allow policy
    return generatePolicy(userId, 'Allow', event.methodArn);
  } catch (error) {
    // Log error here if needed
    return generatePolicy('user', 'Deny', event.methodArn);
  }
}; 