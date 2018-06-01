pipeline {
    agent { label 'linux' && 'docker' }
    stages {
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
    }
}