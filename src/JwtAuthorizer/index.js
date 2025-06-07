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
    if (!token) {
      console.warn('No token provided');
      return null;
    }

    const { payload } = await jwtVerify(token, JWT_SIGNING_KEY, {
      issuer: JWT_ISSUER,
      audience: JWT_AUDIENCE,
      algorithms: ['HS256'],
      clockTolerance: 30, // Allow 30 seconds clock skew
      maxTokenAge: '1h' // Maximum token age
    });

    // Check if token is expired
    const now = Math.floor(Date.now() / 1000);
    if (payload.exp && payload.exp < now) {
      console.warn('Token has expired');
      return null;
    }

    // Check if token is not yet valid
    if (payload.nbf && payload.nbf > now) {
      console.warn('Token is not yet valid');
      return null;
    }

    // Validate required claims
    if (!payload.sub) {
      console.warn('Token missing required subject claim');
      return null;
    }

    return payload;
  } catch (error) {
    if (error.code === 'ERR_JWT_EXPIRED') {
      console.warn('Token has expired');
    } else if (error.code === 'ERR_JWT_NOT_YET_VALID') {
      console.warn('Token is not yet valid');
    } else if (error.code === 'ERR_JWT_INVALID') {
      console.warn('Invalid token');
    } else {
      console.error('Error validating token:', error);
    }
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