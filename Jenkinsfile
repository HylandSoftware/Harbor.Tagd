pipeline {
    agent { label 'linux' && 'docker' }
    stages {
        stage ("Check Helm Chart") {
            agent {
                docker {
                    image 'dtzar/helm-kubectl'
                    label 'linux && docker'
                    reuseNode true
                }
            }

            steps {
                sh 'helm lint --strict ./contrib/helm/tagd/'
            }
        }

        stage("Build & Test") {
            agent {
                docker {
                    image 'hcr.io/library/dotnet:2.0-sdk'
                    label 'linux' && 'docker'
                    reuseNode true
                }
            }
            steps {
                sh '''
                HOME=$WORKSPACE ./build.sh -t dist
                '''
            }
        }

        stage("Build Container") {
            steps {
                sh 'docker build . -t hcr.io/nlowe/tagd:latest'
            }
        }
        
        stage("Publish Image") {
            when {
                branch 'master'
            }
            steps {
                withDockerRegistry([credentialsId: 'hcr-credentials', url: 'https://hcr.io']) {
                    sh 'docker push hcr.io/nlowe/tagd:latest'
                }
            }
        }
        
        stage ("Deploy") {
            agent {
                docker {
                    image 'dtzar/helm-kubectl'
                    label 'linux && docker'
                    reuseNode true
                }
            }

            environment {
                KUBECONFIG = credentials('devops-kubeconfig')
                HARBOR_CREDS = credentials('hcr-credentials')
                WEBHOOK = credentials('webhook')
            }

            when {
                branch 'master'
            }

            steps {
                sh '''
                export HOME=$PWD

                kubectl version
                helm version

                helm upgrade tagd ./contrib/helm/tagd/ --install \
                    --namespace tagd \
                    --set "harbor.username=${HARBOR_CREDS_USR}" \
                    --set "harbor.password=${HARBOR_CREDS_PSW}" \
                    --set "notify.slack=${WEBHOOK}" \
                    --set "verbosity=verbose"
                    --wait
                '''
            }
        }
    }
}